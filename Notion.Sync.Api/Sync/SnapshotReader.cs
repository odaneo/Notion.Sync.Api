using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Notion.Sync.Api.Sync;

public static partial class SnapshotReader
{
    public static PreparedSnapshot Read(SyncRequest request, SyncOptions options)
    {
        Require(request.Complete && request.Articles is not null && request.Tags is not null && request.SubTags is not null,
            "A complete snapshot with articles, tags and subTags arrays is required.");
        Require(request.Repository == options.Repository && request.Branch == options.Branch, "Unexpected repository or branch.");
        Require(request.WorkflowRunNumber > 0, "workflowRunNumber must be positive.");
        Require(request.CommitSha is not null && CommitPattern().IsMatch(request.CommitSha), "Invalid commitSha.");
        Require(request.Articles!.Count <= 5000 && request.Tags!.Count <= 1000 && request.SubTags!.Count <= 1000, "Snapshot is too large.");
        Require(request.Articles.All(article => article is not null) && request.Tags!.All(category => category is not null) &&
            request.SubTags!.All(category => category is not null), "Snapshot arrays must not contain null entries.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var tags = ReadCategories(request.Tags!, "Tags", ids);
        var subTags = ReadCategories(request.SubTags!, "SubTags", ids);
        var ignored = 0;
        var articles = new List<ArticleSnapshot>();
        foreach (var input in request.Articles.Where(article => article.Published))
        {
            var id = ReadId(input.Id, ids);
            var sourcePath = ReadPath(input.SourcePath);
            Require(!sourcePath.StartsWith("Tags/", StringComparison.OrdinalIgnoreCase) &&
                !sourcePath.StartsWith("SubTags/", StringComparison.OrdinalIgnoreCase), "Article path must not be a category path.");
            var title = ReadText(input.Title ?? Path.GetFileNameWithoutExtension(sourcePath), "title");
            var slug = ReadSlug(input.Slug);
            Require(input.Content is not null, "Published articles require content.");
            var edited = ReadTime(input.LastEditedTime);
            var tagIds = Resolve(input.TagRefs, tags, "Tags", ref ignored);
            var subTagIds = Resolve(input.SubTags, subTags, "SubTags", ref ignored);
            var article = new ArticleSnapshot(id, sourcePath, title, slug, input.Description,
                input.Recommend, input.Content!, edited, tagIds, subTagIds, "");
            articles.Add(article with { ContentHash = Hash(article) });
        }
        Unique(articles.Select(article => article.Slug), "article slug");
        Unique(articles.Select(article => article.SourcePath), "article path", StringComparer.OrdinalIgnoreCase);
        return new PreparedSnapshot(options.Repository, request.CommitSha!.ToLowerInvariant(), request.WorkflowRunNumber,
            articles.ToArray(), tags, subTags, ignored);
    }

    public static string Hash(ArticleSnapshot article) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            article.Id, article.SourcePath, article.Title, article.Slug, article.Description,
            article.Recommend, article.Content, article.LastEditedTime,
            TagIds = article.TagIds.Order(StringComparer.Ordinal),
            SubTagIds = article.SubTagIds.Order(StringComparer.Ordinal)
        }))).ToLowerInvariant();

    private static CategorySnapshot[] ReadCategories(List<CategoryInput> inputs, string folder, HashSet<string> ids)
    {
        var result = inputs.Select(input =>
        {
            var sourcePath = ReadPath(input.SourcePath);
            Require(sourcePath.StartsWith(folder + "/", StringComparison.Ordinal), "Unexpected category directory.");
            return new CategorySnapshot(ReadId(input.Id, ids), sourcePath,
                ReadText(Path.GetFileNameWithoutExtension(sourcePath), "category title"), ReadSlug(input.Slug),
                input.Description, folder == "Tags" ? input.LucideIconName : null, ReadTime(input.LastEditedTime));
        }).ToArray();
        Unique(result.Select(category => category.SourcePath), folder + " path", StringComparer.OrdinalIgnoreCase);
        Unique(result.Select(category => category.Slug), folder + " slug");
        return result;
    }

    private static string[] Resolve(List<string?>? references, CategorySnapshot[] categories, string folder, ref int ignored)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in references ?? [])
        {
            var match = reference is null ? Match.Empty : WikiLinkPattern().Match(reference.Trim());
            if (!match.Success) { ignored++; continue; }
            var target = match.Groups[1].Value.Trim();
            if (target.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) target = target[..^3];
            var matches = categories.Where(category => target.Contains('/')
                ? string.Equals(category.SourcePath, target + ".md", StringComparison.OrdinalIgnoreCase)
                : string.Equals(Path.GetFileNameWithoutExtension(category.SourcePath), target, StringComparison.OrdinalIgnoreCase))
                .Take(2).ToArray();
            if (target.Contains('/') && !target.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) || matches.Length != 1)
            { ignored++; continue; }
            result.Add(matches[0].Id);
        }
        return result.Order(StringComparer.Ordinal).ToArray();
    }

    private static string ReadId(string? value, HashSet<string> ids)
    {
        Require(value is not null && (Guid.TryParseExact(value, "N", out _) || Guid.TryParseExact(value, "D", out _)), "Invalid note UUID.");
        var id = Guid.Parse(value!).ToString("N");
        Require(id != Guid.Empty.ToString("N") && ids.Add(id), "Empty or duplicate note UUID.");
        return id;
    }

    private static string ReadPath(string? value)
    {
        Require(!string.IsNullOrWhiteSpace(value) && value.Length <= 1024 && value.EndsWith(".md", StringComparison.OrdinalIgnoreCase), "Invalid sourcePath.");
        Require(!value!.Contains('\\') && !value.Contains(':') && !value.Contains('\0') &&
            value.Split('/').All(segment => segment.Length > 0 && !segment.StartsWith('.')), "sourcePath must be a safe vault-relative Markdown path.");
        return value;
    }

    private static string ReadText(string? value, string field)
    {
        Require(!string.IsNullOrWhiteSpace(value) && value.Length <= 255, field + " must contain 1–255 characters.");
        return value!.Trim();
    }

    private static string ReadSlug(string? value)
    {
        var slug = ReadText(value, "slug");
        Require(!slug.Any(character => "/\\?#".Contains(character) || char.IsControl(character)), "Invalid slug.");
        return slug;
    }

    private static DateTime ReadTime(DateTimeOffset? value)
    {
        Require(value.HasValue && value.Value.Year >= 1970, "A Git lastEditedTime is required.");
        var time = value!.Value.UtcDateTime;
        return new DateTime(time.Ticks - time.Ticks % 10, DateTimeKind.Utc);
    }

    private static void Unique(IEnumerable<string> values, string field, IEqualityComparer<string>? comparer = null)
    {
        var seen = new HashSet<string>(comparer ?? StringComparer.Ordinal);
        Require(values.All(seen.Add), "Duplicate " + field + ".");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new SyncValidationException(message);
    }

    [GeneratedRegex("^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$")]
    private static partial Regex CommitPattern();
    [GeneratedRegex("^\\[\\[([^\\]|#]+)(?:\\|[^\\]]*)?\\]\\]$")]
    private static partial Regex WikiLinkPattern();
}
