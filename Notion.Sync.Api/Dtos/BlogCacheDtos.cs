namespace Notion.Sync.Api.Dtos
{
    public record BlogCacheSubTagDto(
        string Id,
        string Title,
        string Slug
    );

    public record BlogCacheTagDto(
        string Id,
        string Slug,
        string Title,
        int ArticleCount,
        string Description,
        string LucideIconName
    );

    public record BlogCacheArticleSummaryDto(
        string Id,
        string Title,
        string Slug,
        DateTime LastEditedTime,
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheSubTagDto> SubTags
    );

    public record BlogCacheArticleDetailDto(
        string Content,
        string Title,
        string Slug,
        DateTime LastEditedTime,
        string Id,
        string Description,
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheSubTagDto> SubTags
    );

    public record BlogCacheHomeDto(
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheArticleSummaryDto> RecommendArticles
    );

    public record BlogCacheTagWithArticlesDto(
        string Slug,
        string Title,
        IReadOnlyList<BlogCacheArticleSummaryDto> Articles
    );

    public record BlogCacheTagDetailDto(
        BlogCacheTagDto Tag,
        IReadOnlyList<BlogCacheArticleSummaryDto> Articles
    );

    public record BlogCacheManifestDto(
        string Version,
        DateTime GeneratedAt,
        int ArticleCount,
        int TagCount,
        IReadOnlyList<string> Keys
    );
}
