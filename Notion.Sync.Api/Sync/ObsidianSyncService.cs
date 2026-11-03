using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Sync;

public sealed class ObsidianSyncService(AppDbContext db, RevalidationClient cache,
    IOptions<SyncOptions> options, IConfiguration configuration, ILogger<ObsidianSyncService> logger)
{
    public async Task<SyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken)
    {
        var snapshot = SnapshotReader.Read(request, options.Value);
        SyncResult result;
        bool refresh;
        try { (result, refresh) = await ApplyAsync(snapshot, cancellationToken); }
        catch (SyncConflictException) { throw; }
        catch (Exception exception)
        {
            await RecordFailureAsync(snapshot, exception);
            throw;
        }
        if (!refresh) return result;

        try { await cache.RefreshAsync(snapshot.CommitSha, cancellationToken); }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or CacheRefreshException)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var message = exception is CacheRefreshException ? exception.Message : "Cache refresh request failed or timed out.";
            await db.SyncRuns.Where(run => run.Repository == snapshot.Repository && run.CommitSha == snapshot.CommitSha &&
                    run.WorkflowRunNumber <= snapshot.WorkflowRunNumber && run.RequiresRevalidation)
                .ExecuteUpdateAsync(setters => setters.SetProperty(run => run.LastError, message), timeout.Token);
            logger.LogWarning("Cache refresh pending for commit {CommitSha}", snapshot.CommitSha);
            throw new CachePendingException(result);
        }

        // An earlier callback must never acknowledge a later commit's pending invalidation.
        await db.SyncRuns.Where(run => run.Repository == snapshot.Repository && run.DatabaseAppliedAt != null &&
                run.WorkflowRunNumber <= snapshot.WorkflowRunNumber && run.RequiresRevalidation)
            .ExecuteUpdateAsync(setters => setters.SetProperty(run => run.RequiresRevalidation, false)
                .SetProperty(run => run.CacheInvalidatedAt, DateTime.UtcNow)
                .SetProperty(run => run.LastError, (string?)null), cancellationToken);
        return result with { CacheRefreshed = true };
    }

    private async Task<(SyncResult Result, bool Refresh)> ApplyAsync(PreparedSnapshot snapshot, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var latest = await db.SyncRuns.AsNoTracking().Where(run => run.Repository == snapshot.Repository && run.DatabaseAppliedAt != null)
            .OrderByDescending(run => run.WorkflowRunNumber).FirstOrDefaultAsync(ct);
        if (latest is not null && snapshot.WorkflowRunNumber < latest.WorkflowRunNumber)
            return (new SyncResult("superseded", snapshot.CommitSha), false);
        if (latest is not null && snapshot.WorkflowRunNumber == latest.WorkflowRunNumber && snapshot.CommitSha != latest.CommitSha)
            throw new SyncConflictException("This workflow run number already belongs to another commit.");

        var run = await db.SyncRuns.SingleOrDefaultAsync(item => item.Repository == snapshot.Repository && item.CommitSha == snapshot.CommitSha, ct);
        var pending = await db.SyncRuns.AnyAsync(item => item.Repository == snapshot.Repository && item.RequiresRevalidation, ct);
        var articles = await db.Articles.ToDictionaryAsync(article => article.Id, ct);
        var tags = await db.Tags.ToDictionaryAsync(tag => tag.Id, ct);
        var subTags = await db.SubTags.ToDictionaryAsync(tag => tag.Id, ct);
        var articleTags = (await db.ArticleTags.AsNoTracking().ToListAsync(ct)).ToLookup(link => link.ArticleId, link => link.TagId);
        var articleSubTags = (await db.ArticleSubTags.AsNoTracking().ToListAsync(ct)).ToLookup(link => link.ArticleId, link => link.SubTagId);
        var incomingArticles = snapshot.Articles.ToDictionary(article => article.Id);
        var incomingTags = snapshot.Tags.ToDictionary(tag => tag.Id);
        var incomingSubTags = snapshot.SubTags.ToDictionary(tag => tag.Id);

        var removedArticles = articles.Values.Where(article => !incomingArticles.ContainsKey(article.Id)).ToArray();
        var removedTags = tags.Values.Where(tag => !incomingTags.ContainsKey(tag.Id)).ToArray();
        var removedSubTags = subTags.Values.Where(tag => !incomingSubTags.ContainsKey(tag.Id)).ToArray();
        var changedArticles = snapshot.Articles.Where(article => articles.TryGetValue(article.Id, out var current) &&
            (!ArticleMatches(current, article) || !article.TagIds.ToHashSet().SetEquals(articleTags[article.Id]) ||
             !article.SubTagIds.ToHashSet().SetEquals(articleSubTags[article.Id]))).Select(article => article.Id).ToHashSet();
        var changedTags = snapshot.Tags.Where(tag => tags.TryGetValue(tag.Id, out var current) && !TagMatches(current, tag)).Select(tag => tag.Id).ToHashSet();
        var changedSubTags = snapshot.SubTags.Where(tag => subTags.TryGetValue(tag.Id, out var current) && !SubTagMatches(current, tag)).Select(tag => tag.Id).ToHashSet();
        var added = snapshot.Articles.Count(article => !articles.ContainsKey(article.Id));
        var tagsChanged = removedTags.Length + changedTags.Count + snapshot.Tags.Count(tag => !tags.ContainsKey(tag.Id));
        var subTagsChanged = removedSubTags.Length + changedSubTags.Count + snapshot.SubTags.Count(tag => !subTags.ContainsKey(tag.Id));
        var changed = added + removedArticles.Length + changedArticles.Count + tagsChanged + subTagsChanged > 0;

        db.Articles.RemoveRange(removedArticles);
        db.Tags.RemoveRange(removedTags);
        db.SubTags.RemoveRange(removedSubTags);
        // Free unique slugs/paths inside the same transaction, including two notes swapping names.
        var temporary = "__sync_" + Guid.NewGuid().ToString("N") + "_";
        foreach (var input in snapshot.Articles.Where(article => articles.ContainsKey(article.Id)))
        {
            var current = articles[input.Id];
            if (current.Slug != input.Slug) current.Slug = temporary + input.Id;
            if (current.SourcePath != input.SourcePath) current.SourcePath = temporary + input.Id + ".md";
        }
        foreach (var input in snapshot.Tags.Where(tag => tags.ContainsKey(tag.Id)))
            if (tags[input.Id].Slug != input.Slug) tags[input.Id].Slug = temporary + input.Id;
        foreach (var input in snapshot.SubTags.Where(tag => subTags.ContainsKey(tag.Id)))
            if (subTags[input.Id].Slug != input.Slug) subTags[input.Id].Slug = temporary + input.Id;
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);

        foreach (var input in snapshot.Tags)
        {
            if (!tags.TryGetValue(input.Id, out var tag))
            {
                tag = new Tag { Id = input.Id, Title = input.Title, Slug = input.Slug };
                db.Tags.Add(tag);
            }
            else if (!changedTags.Contains(input.Id)) continue;
            tag.Title = input.Title; tag.Slug = input.Slug; tag.Description = input.Description;
            tag.LucideIconName = input.LucideIconName; tag.LastEditedTime = input.LastEditedTime;
        }
        foreach (var input in snapshot.SubTags)
        {
            if (!subTags.TryGetValue(input.Id, out var tag))
            {
                tag = new SubTag { Id = input.Id, Title = input.Title, Slug = input.Slug };
                db.SubTags.Add(tag);
            }
            else if (!changedSubTags.Contains(input.Id)) continue;
            tag.Title = input.Title; tag.Slug = input.Slug; tag.LastEditedTime = input.LastEditedTime;
        }
        foreach (var input in snapshot.Articles)
        {
            if (!articles.TryGetValue(input.Id, out var article))
            {
                article = new Article { Id = input.Id, Title = input.Title, Slug = input.Slug,
                    Content = input.Content, SourcePath = input.SourcePath, ContentHash = input.ContentHash };
                db.Articles.Add(article);
            }
            else if (!changedArticles.Contains(input.Id)) continue;
            article.Title = input.Title; article.Slug = input.Slug; article.Description = input.Description;
            article.Content = input.Content; article.SourcePath = input.SourcePath;
            article.ContentHash = input.ContentHash; article.Recommend = input.Recommend;
            article.LastEditedTime = input.LastEditedTime;
            await db.ArticleTags.Where(link => link.ArticleId == input.Id && !input.TagIds.Contains(link.TagId)).ExecuteDeleteAsync(ct);
            await db.ArticleSubTags.Where(link => link.ArticleId == input.Id && !input.SubTagIds.Contains(link.SubTagId)).ExecuteDeleteAsync(ct);
            foreach (var id in input.TagIds.Except(articleTags[input.Id])) db.ArticleTags.Add(new ArticleTag { ArticleId = input.Id, TagId = id });
            foreach (var id in input.SubTagIds.Except(articleSubTags[input.Id])) db.ArticleSubTags.Add(new ArticleSubTag { ArticleId = input.Id, SubTagId = id });
        }

        if (run is null)
        {
            run = new SyncRun { Repository = snapshot.Repository, CommitSha = snapshot.CommitSha, StartedAt = DateTime.UtcNow };
            db.SyncRuns.Add(run);
        }
        run.WorkflowRunNumber = snapshot.WorkflowRunNumber;
        run.DatabaseAppliedAt = DateTime.UtcNow;
        run.RequiresRevalidation = changed || pending;
        if (run.RequiresRevalidation) run.CacheInvalidatedAt = null;
        run.LastError = null;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (new SyncResult(changed ? "applied" : "unchanged", snapshot.CommitSha, added,
            changedArticles.Count, removedArticles.Length, tagsChanged, subTagsChanged, snapshot.IgnoredReferences), run.RequiresRevalidation);
    }

    private async Task LockAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '10s'", ct);
        var environmentKey = configuration["Database:Schema"] == "blog_test" ? 1 : 2;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({0x4F425349}, {environmentKey})", ct);
    }

    private async Task RecordFailureAsync(PreparedSnapshot snapshot, Exception exception)
    {
        db.ChangeTracker.Clear();
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var transaction = await db.Database.BeginTransactionAsync(timeout.Token);
            await LockAsync(timeout.Token);
            var run = await db.SyncRuns.SingleOrDefaultAsync(item => item.Repository == snapshot.Repository && item.CommitSha == snapshot.CommitSha, timeout.Token);
            if (run?.DatabaseAppliedAt is not null) return;
            if (run is null)
            {
                run = new SyncRun { Repository = snapshot.Repository, CommitSha = snapshot.CommitSha, StartedAt = DateTime.UtcNow };
                db.SyncRuns.Add(run);
            }
            run.WorkflowRunNumber = Math.Max(run.WorkflowRunNumber, snapshot.WorkflowRunNumber);
            run.LastError = "Database transaction failed (" + exception.GetType().Name + ").";
            await db.SaveChangesAsync(timeout.Token);
            await transaction.CommitAsync(timeout.Token);
        }
        catch (Exception recordingFailure) { logger.LogError(recordingFailure, "Could not record failed synchronization"); }
    }

    private static bool ArticleMatches(Article current, ArticleSnapshot input) =>
        current.ContentHash == input.ContentHash && current.Title == input.Title && current.Slug == input.Slug &&
        current.Description == input.Description && current.Content == input.Content && current.SourcePath == input.SourcePath &&
        current.Recommend == input.Recommend && current.LastEditedTime == input.LastEditedTime;
    private static bool TagMatches(Tag current, CategorySnapshot input) => current.Title == input.Title && current.Slug == input.Slug &&
        current.Description == input.Description && current.LucideIconName == input.LucideIconName && current.LastEditedTime == input.LastEditedTime;
    private static bool SubTagMatches(SubTag current, CategorySnapshot input) => current.Title == input.Title && current.Slug == input.Slug &&
        current.LastEditedTime == input.LastEditedTime;
}
