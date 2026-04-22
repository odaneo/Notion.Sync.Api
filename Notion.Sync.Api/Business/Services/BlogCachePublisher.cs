using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Business.Services
{
    public class BlogCachePublisher(
        AppDbContext dbContext,
        ICloudflareKvClient cloudflareKvClient,
        ILogger<BlogCachePublisher> logger) : IBlogCachePublisher
    {
        private const int VersionedKeyExpirationTtlSeconds = 604800;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task PublishAsync(CancellationToken cancellationToken)
        {
            try
            {
                var generatedAt = DateTime.UtcNow;
                var version = generatedAt.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
                var prefix = $"blog:v:{version}";

                var articles = await dbContext.NotionArticles
                    .AsNoTracking()
                    .Where(article => article.Published)
                    .Include(article => article.Article)
                    .Include(article => article.NotionArticleTags)
                        .ThenInclude(articleTag => articleTag.Tag)
                    .Include(article => article.NotionArticleSubTags)
                        .ThenInclude(articleSubTag => articleSubTag.SubTag)
                    .AsSplitQuery()
                    .OrderByDescending(article => article.LastEditedTime)
                    .ToListAsync(cancellationToken);

                var tags = await dbContext.Tags
                    .AsNoTracking()
                    .OrderBy(tag => tag.Title)
                    .ToListAsync(cancellationToken);

                var tagDtos = tags
                    .Select(tag => ToTagDto(tag, articles))
                    .Where(tag => tag.ArticleCount > 0)
                    .ToList();

                var articleSummaries = articles
                    .Select(article => ToArticleSummaryDto(article, tagDtos))
                    .ToList();

                var home = new BlogCacheHomeDto(
                    tagDtos,
                    articles
                        .Where(article => article.Recommend)
                        .Select(article => ToArticleSummaryDto(article, tagDtos))
                        .ToList()
                );

                var tagList = tagDtos
                    .Select(tag => new BlogCacheTagWithArticlesDto(
                        tag.Slug,
                        tag.Title,
                        articleSummaries
                            .Where(article => article.Tags.Any(articleTag => articleTag.Id == tag.Id))
                            .ToList()
                    ))
                    .ToList();

                var versionedPayloads = new List<(string Key, object Value)>
                {
                    ($"{prefix}:home", home),
                    ($"{prefix}:articles", articleSummaries),
                    ($"{prefix}:tags", tagList)
                };

                foreach (var tag in tagDtos)
                {
                    var tagArticles = articleSummaries
                        .Where(article => article.Tags.Any(articleTag => articleTag.Id == tag.Id))
                        .ToList();

                    versionedPayloads.Add(($"{prefix}:tag:{tag.Slug}", new BlogCacheTagDetailDto(tag, tagArticles)));
                }

                foreach (var article in articles)
                {
                    versionedPayloads.Add(($"{prefix}:article:{article.Slug}", ToArticleDetailDto(article, tagDtos)));
                }

                var manifestKey = $"{prefix}:manifest";
                var manifestKeys = versionedPayloads
                    .Select(payload => payload.Key)
                    .Append(manifestKey)
                    .ToList();

                foreach (var payload in versionedPayloads)
                {
                    await PutJsonAsync(payload.Key, payload.Value, cancellationToken);
                }

                var manifest = new BlogCacheManifestDto(
                    version,
                    generatedAt,
                    articles.Count,
                    tagDtos.Count,
                    manifestKeys
                );

                await PutJsonAsync(manifestKey, manifest, cancellationToken);
                await cloudflareKvClient.PutTextAsync("blog:active", version, cancellationToken);

                logger.LogInformation(
                    "Published blog cache version {Version}. Articles: {ArticleCount}, Tags: {TagCount}",
                    version,
                    articles.Count,
                    tagDtos.Count
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish blog cache to Cloudflare KV.");
                throw;
            }
        }

        private static BlogCacheTagDto ToTagDto(Tag tag, List<NotionArticle> publishedArticles)
        {
            var articleCount = publishedArticles.Count(article =>
                article.NotionArticleTags.Any(articleTag => articleTag.TagId == tag.Id)
            );

            return new BlogCacheTagDto(
                tag.Id,
                tag.Slug ?? string.Empty,
                tag.Title ?? string.Empty,
                articleCount,
                tag.Description ?? string.Empty,
                tag.LucideIconName ?? string.Empty
            );
        }

        private static BlogCacheArticleSummaryDto ToArticleSummaryDto(
            NotionArticle article,
            IReadOnlyList<BlogCacheTagDto> tags)
        {
            return new BlogCacheArticleSummaryDto(
                article.Id,
                article.Title,
                article.Slug,
                article.LastEditedTime,
                article.NotionArticleTags
                    .Select(articleTag => tags.First(tag => tag.Id == articleTag.TagId))
                    .ToList(),
                article.NotionArticleSubTags
                    .Select(articleSubTag => new BlogCacheSubTagDto(
                        articleSubTag.SubTag.Id,
                        articleSubTag.SubTag.Title ?? string.Empty,
                        articleSubTag.SubTag.Slug ?? string.Empty
                    ))
                    .ToList()
            );
        }

        private static BlogCacheArticleDetailDto ToArticleDetailDto(
            NotionArticle article,
            IReadOnlyList<BlogCacheTagDto> tags)
        {
            var summary = ToArticleSummaryDto(article, tags);

            return new BlogCacheArticleDetailDto(
                article.Article?.Content ?? string.Empty,
                article.Title,
                article.Slug,
                article.LastEditedTime,
                article.Id,
                article.Description ?? string.Empty,
                summary.Tags,
                summary.SubTags
            );
        }

        private async Task PutJsonAsync(string key, object value, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await cloudflareKvClient.PutJsonAsync(
                key,
                json,
                VersionedKeyExpirationTtlSeconds,
                cancellationToken
            );
        }
    }
}
