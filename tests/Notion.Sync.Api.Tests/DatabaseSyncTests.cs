using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Sync;
using Xunit;

namespace Notion.Sync.Api.Tests;

public sealed class DatabaseSyncTests
{
    [DatabaseFact]
    public async Task FullSnapshotsDiffRollbackResumeAndRejectOldConcurrentRequests()
    {
        var root = Path.GetFullPath("../../../../../", AppContext.BaseDirectory);
        var configuration = new ConfigurationBuilder().SetBasePath(Path.Combine(root, "Notion.Sync.Api"))
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json").AddUserSecrets<AppDbContext>()
            .AddInMemoryCollection(new Dictionary<string, string?>
            { ["Database:RootCertificate"] = Path.Combine(root, "Notion.Sync.Api/Certificates/supabase-ca.crt") }).Build();
        Assert.Equal("blog_test", configuration.GetDatabaseSchema());
        AppDbContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(configuration.BuildFinalConnString()).AddInterceptors(interceptors).Options);
        await using (var check = Context())
        {
            Assert.False(await check.Articles.AnyAsync() || await check.Tags.AnyAsync() || await check.SubTags.AnyAsync(),
                "Database tests require empty blog_test business tables; no existing content will be cleared.");
        }

        const string secondArticle = "44444444444444448444444444444444";
        const string secondTag = "55555555555545558555555555555555";
        const string secondSubTag = "66666666666646668666666666666666";
        var calls = 0;
        var failCache = false;
        string? delayedCommit = null;
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<SyncResult> Apply(SyncRequest request, params IInterceptor[] interceptors)
        {
            await using var context = Context(interceptors);
            using var http = new HttpClient(new CallbackHandler(async (request, cancellationToken) =>
            {
                Interlocked.Increment(ref calls);
                var payload = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
                if (payload.GetProperty("commitSha").GetString() == delayedCommit)
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.WaitAsync(cancellationToken);
                }
                return new HttpResponseMessage(failCache ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                    { Content = new StringContent("{\"revalidated\":true}") };
            }));
            var cache = new RevalidationClient(http, Options.Create(new RevalidationOptions
                { Url = "http://localhost:3000/api/revalidate", Token = SnapshotTests.Settings.Token }));
            return await new ObsidianSyncService(context, cache, Options.Create(SnapshotTests.Settings), configuration,
                NullLogger<ObsidianSyncService>.Instance).SynchronizeAsync(request, default);
        }
        SyncRequest Snapshot(long number, string content = "# Markdown", bool two = true, bool swapped = false, bool unclassified = false, bool reorder = false) =>
            SnapshotTests.Request(number, articles:
            [
                SnapshotTests.Article(content, tags: unclassified ? ["[[missing]]"] : reorder ? ["[[前端]]", "[[Tags/后端|后端]]"] : ["[[后端]]", "[[前端]]"],
                    subTags: unclassified ? ["[[missing]]"] : ["[[CSharp]]"], slug: swapped ? "second" : "example", sourcePath: swapped ? "知识/另一篇.md" : "知识/示例.md"),
                .. two ? new[] { SnapshotTests.Article(id: secondArticle, tags: ["[[前端]]"], subTags: ["[[CSharp]]", "[[Session]]"],
                    slug: swapped ? "example" : "second", sourcePath: swapped ? "知识/示例.md" : "知识/另一篇.md") } : [],
                new ArticleInput { Published = false, Id = "invalid-draft-id" }
            ],
            tags: [SnapshotTests.Category(SnapshotTests.TagId, "Tags/后端.md", swapped ? "frontend" : "backend"),
                SnapshotTests.Category(secondTag, "Tags/前端.md", swapped ? "backend" : "frontend")],
            subTags: [SnapshotTests.Category(SnapshotTests.SubTagId, "SubTags/CSharp.md", "c-sharp"),
                SnapshotTests.Category(secondSubTag, "SubTags/Session.md", "session")]);
        try
        {
            var first = await Apply(Snapshot(1));
            Assert.Equal(2, first.Added); Assert.True(first.CacheRefreshed);
            Assert.Equal("unchanged", (await Apply(Snapshot(1))).Status);
            await using (var check = Context()) Assert.NotNull((await check.SyncRuns.SingleAsync(run =>
                run.Repository == SnapshotTests.Settings.Repository && run.WorkflowRunNumber == 1)).CacheInvalidatedAt);
            int[] associations;
            await using (var check = Context()) associations = await check.ArticleTags.OrderBy(link => link.Id).Select(link => link.Id).ToArrayAsync();
            var unchanged = await Apply(Snapshot(2, reorder: true));
            Assert.Equal("unchanged", unchanged.Status); Assert.Equal(0, unchanged.Updated); Assert.False(unchanged.CacheRefreshed);
            await using (var check = Context()) Assert.Equal(associations, await check.ArticleTags.OrderBy(link => link.Id).Select(link => link.Id).ToArrayAsync());
            Assert.Equal(1, calls);

            Assert.Equal(1, (await Apply(Snapshot(3, "modified"))).Updated);
            var swapped = await Apply(Snapshot(4, "modified", swapped: true));
            Assert.Equal(2, swapped.Updated); Assert.Equal(2, swapped.TagsChanged);
            var removed = await Apply(Snapshot(5, "modified", two: false, swapped: true, unclassified: true));
            Assert.Equal(1, removed.Deleted); Assert.Equal(2, removed.IgnoredReferences);
            await using (var check = Context())
            {
                Assert.Single(await check.Articles.ToArrayAsync());
                Assert.Empty(await check.ArticleTags.ToArrayAsync()); Assert.Empty(await check.ArticleSubTags.ToArrayAsync());
                Assert.Equal(2, await check.Tags.CountAsync());
            }

            var retry = Snapshot(6, "cache failure", two: false, swapped: true, unclassified: true);
            failCache = true;
            await Assert.ThrowsAsync<CachePendingException>(() => Apply(retry));
            await using (var check = Context())
            {
                Assert.Equal("cache failure", (await check.Articles.SingleAsync()).Content);
                Assert.True((await check.SyncRuns.SingleAsync(run => run.Repository == SnapshotTests.Settings.Repository && run.WorkflowRunNumber == 6)).RequiresRevalidation);
            }
            failCache = false;
            var resumed = await Apply(retry);
            Assert.Equal(0, resumed.Updated); Assert.True(resumed.CacheRefreshed);
            await using (var check = Context()) Assert.False(await check.SyncRuns.AnyAsync(run => run.Repository == SnapshotTests.Settings.Repository && run.RequiresRevalidation));
            var beforeOld = calls;
            Assert.Equal("superseded", (await Apply(Snapshot(1))).Status); Assert.Equal(beforeOld, calls);

            await Task.WhenAll(Apply(Snapshot(7, "seven", two: false, swapped: true, unclassified: true)),
                Apply(Snapshot(8, "eight", two: false, swapped: true, unclassified: true)));
            await using (var check = Context()) Assert.Equal("eight", (await check.Articles.SingleAsync()).Content);

            var older = Snapshot(9, "nine", two: false, swapped: true, unclassified: true);
            var newer = Snapshot(10, "ten", two: false, swapped: true, unclassified: true);
            delayedCommit = older.CommitSha;
            var olderCall = Apply(older);
            try
            {
                await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                failCache = true;
                await Assert.ThrowsAsync<CachePendingException>(() => Apply(newer));
            }
            finally { failCache = false; releaseCallback.TrySetResult(); await olderCall; delayedCommit = null; }
            await using (var check = Context())
                Assert.True((await check.SyncRuns.SingleAsync(run => run.Repository == SnapshotTests.Settings.Repository && run.WorkflowRunNumber == 10)).RequiresRevalidation);
            Assert.True((await Apply(newer)).CacheRefreshed);

            var beforeFailure = calls;
            await Assert.ThrowsAsync<InvalidOperationException>(() => Apply(Snapshot(11, "must roll back", two: false), new FailSecondSave()));
            await using (var check = Context())
            {
                var article = await check.Articles.SingleAsync();
                Assert.Equal("ten", article.Content); Assert.Equal("second", article.Slug); Assert.Equal("知识/另一篇.md", article.SourcePath);
                var failed = await check.SyncRuns.SingleAsync(run => run.Repository == SnapshotTests.Settings.Repository && run.WorkflowRunNumber == 11);
                Assert.Null(failed.DatabaseAppliedAt); Assert.NotNull(failed.LastError);
            }
            Assert.Equal(beforeFailure, calls);
            await Assert.ThrowsAsync<SyncValidationException>(() => Apply(SnapshotTests.Request(12, complete: false)));
            await using (var check = Context()) Assert.Single(await check.Articles.ToArrayAsync());
            Assert.Equal(1, (await Apply(SnapshotTests.Request(12, articles: [], tags: [], subTags: []))).Deleted);
            var afterEmpty = calls;
            Assert.Equal("unchanged", (await Apply(SnapshotTests.Request(13, articles: [], tags: [], subTags: []))).Status);
            Assert.Equal(afterEmpty, calls);
        }
        finally
        {
            await using var cleanup = Context();
            var articleIds = new[] { SnapshotTests.ArticleId, secondArticle };
            var tagIds = new[] { SnapshotTests.TagId, secondTag };
            var subTagIds = new[] { SnapshotTests.SubTagId, secondSubTag };
            await cleanup.Articles.Where(article => articleIds.Contains(article.Id)).ExecuteDeleteAsync();
            await cleanup.Tags.Where(tag => tagIds.Contains(tag.Id)).ExecuteDeleteAsync();
            await cleanup.SubTags.Where(tag => subTagIds.Contains(tag.Id)).ExecuteDeleteAsync();
            await cleanup.SyncRuns.Where(run => run.Repository == SnapshotTests.Settings.Repository).ExecuteDeleteAsync();
        }
    }

    private sealed class FailSecondSave : SaveChangesInterceptor
    {
        private int saves;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (++saves == 2) throw new InvalidOperationException("Injected failure after temporary identifiers were saved.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_DATABASE_TESTS") != "1")
            Skip = "Set RUN_DATABASE_TESTS=1 to verify an empty blog_test with local User Secrets.";
    }
}
