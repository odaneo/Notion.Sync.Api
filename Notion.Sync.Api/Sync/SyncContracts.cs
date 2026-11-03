namespace Notion.Sync.Api.Sync;

public sealed class SyncRequest
{
    public string? Repository { get; init; }
    public string? Branch { get; init; }
    public string? CommitSha { get; init; }
    public long WorkflowRunNumber { get; init; }
    public bool Complete { get; init; }
    public List<ArticleInput>? Articles { get; init; }
    public List<CategoryInput>? Tags { get; init; }
    public List<CategoryInput>? SubTags { get; init; }
}

public sealed class ArticleInput
{
    public bool Published { get; init; }
    public string? Id { get; init; }
    public string? SourcePath { get; init; }
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public bool Recommend { get; init; }
    public string? Content { get; init; }
    public DateTimeOffset? LastEditedTime { get; init; }
    public List<string?>? TagRefs { get; init; }
    public List<string?>? SubTags { get; init; }
}

public sealed class CategoryInput
{
    public string? Id { get; init; }
    public string? SourcePath { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? LucideIconName { get; init; }
    public DateTimeOffset? LastEditedTime { get; init; }
}

public sealed record CategorySnapshot(string Id, string SourcePath, string Title, string Slug,
    string? Description, string? LucideIconName, DateTime LastEditedTime);

public sealed record ArticleSnapshot(string Id, string SourcePath, string Title, string Slug,
    string? Description, bool Recommend, string Content, DateTime LastEditedTime,
    string[] TagIds, string[] SubTagIds, string ContentHash);

public sealed record PreparedSnapshot(string Repository, string CommitSha, long WorkflowRunNumber,
    ArticleSnapshot[] Articles, CategorySnapshot[] Tags, CategorySnapshot[] SubTags, int IgnoredReferences);

public sealed record SyncResult(string Status, string CommitSha, int Added = 0, int Updated = 0,
    int Deleted = 0, int TagsChanged = 0, int SubTagsChanged = 0,
    int IgnoredReferences = 0, bool CacheRefreshed = false);

public sealed class SyncValidationException(string message) : Exception(message);
public sealed class SyncConflictException(string message) : Exception(message);
public sealed class CacheRefreshException(string message) : Exception(message);

public sealed class CachePendingException(SyncResult result) : Exception("Database applied; cache refresh is pending.")
{
    public SyncResult Result { get; } = result;
}
