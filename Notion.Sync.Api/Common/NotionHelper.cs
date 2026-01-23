using Notion.Sync.Api.Dtos;
using System.Text.Json;

namespace Notion.Sync.Api.Common
{
    public static class NotionHelper
    {
        public static ICollection<NotionArticleDto> GetNotionArticleDtoList(JsonElement articlesJson)
        {
            ICollection<NotionArticleDto> notionArticleList = [];

            foreach (var result in articlesJson.EnumerateArray())
            {
                var articleId = GetArticleIdFromJson(result);
                var title = GetArticleTitleFromJson(result);
                var slug = GetSlugFromJson(result);
                bool published = GetIsPublishedFromJson(result);
                bool recommend = GetIsRecommendFromJson(result);
                ICollection<string?> tagsIds = GetTagIdsFromJson(result).ToList();
                ICollection<string?> subTagsIds = GetSubTagIdsFromJson(result).ToList();

                notionArticleList.Add(
                    new NotionArticleDto()
                    {
                        NotionId = GetNotionIdFromJson(result),
                        ArticleId = articleId,
                        Title = title,
                        Slug = slug,
                        Published = published,
                        Recommend = recommend,
                        LastEditedTime = GetLastEditedTimeFromJson(result),
                        TagsIds = tagsIds,
                        SubTagsIds = subTagsIds
                    });
            }

            return notionArticleList;
        }
        public static ICollection<TagDto> GetTagDtoList(JsonElement tagJson, ICollection<SubTagDto> subTagDtoList)
        {
            ICollection<TagDto> tagDtoList = [];

            var subTagMap = subTagDtoList.ToDictionary(t => t.NotionId!);

            foreach (var result in tagJson.EnumerateArray())
            {
                var slug = GetSlugFromJson(result);
                var title = GetTitleFromJson(result);
                var lucideIconName = GetLucideIconNameFromJson(result);
                ICollection<SubTagDto> subTags = GetSubTagIdsFromJson(result)
                            .Where(id => id != null && subTagMap.TryGetValue(id, out _))
                            .Select(id => subTagMap[id!])
                            .ToList();

                var tagDto = new TagDto()
                {
                    NotionId = GetNotionIdFromJson(result),
                    Title = title,
                    Slug = slug,
                    LucideIconName = lucideIconName,
                    LastEditedTime = GetLastEditedTimeFromJson(result),
                    SubTags = subTags
                };

                tagDtoList.Add(tagDto);
            }

            return tagDtoList;
        }
        public static ICollection<SubTagDto> GetSubTagDtoList(JsonElement subTagsJson)
        {
            ICollection<SubTagDto> subTagDtoList = [];

            foreach (var result in subTagsJson.EnumerateArray())
            {
                var slug = GetSlugFromJson(result);
                var title = GetTitleFromJson(result);

                var subTagDto = new SubTagDto()
                {
                    NotionId = GetNotionIdFromJson(result),
                    Title = title,
                    Slug = slug,
                    LastEditedTime = GetLastEditedTimeFromJson(result)
                };
                subTagDtoList.Add(subTagDto);
            }

            return subTagDtoList;
        }

        private static string? GetSlugFromJson(JsonElement result)
        {
            return result
                    .GetProperty("properties")
                    .GetProperty("Slug")
                    .GetProperty("rich_text")
                    .EnumerateArray()
                    .FirstOrDefault()
                    .GetProperty("plain_text").GetString();
        }
        private static string? GetLucideIconNameFromJson(JsonElement result)
        {
            return result
                    .GetProperty("properties")
                    .GetProperty("LucideIconName")
                    .GetProperty("rich_text")
                    .EnumerateArray()
                    .FirstOrDefault()
                    .GetProperty("plain_text").GetString();
        }
        private static string? GetArticleTitleFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                    .GetProperty("Title")
                    .GetProperty("rich_text")
                    .EnumerateArray()
                    .FirstOrDefault()
                    .GetProperty("plain_text").GetString();
        }
        private static string? GetTitleFromJson(JsonElement result)
        {
            var arr = result.GetProperty("properties")
                    .GetProperty("Name")
                    .GetProperty("title")
                    .EnumerateArray();
            return string.Join("", arr.Select(x => x.GetProperty("plain_text").GetString()));
        }
        private static string? GetNotionIdFromJson(JsonElement result)
        {
            return result.GetProperty("id").GetString();
        }
        private static DateTime GetLastEditedTimeFromJson(JsonElement result)
        {
            return result.GetProperty("last_edited_time").GetDateTime();
        }
        private static string? GetArticleIdFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                        .GetProperty("Name")
                        .GetProperty("title")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetProperty("mention")
                        .GetProperty("page")
                        .GetProperty("id").GetString();
        }
        private static bool GetIsPublishedFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                      .GetProperty("Published")
                      .GetProperty("select")
                      .GetProperty("name").ToString() == "发布";
        }
        private static bool GetIsRecommendFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                      .GetProperty("Recommend")
                      .GetProperty("select")
                      .GetProperty("name").ToString() == "True";
        }
        private static IEnumerable<string?> GetTagIdsFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                     .GetProperty("🏷️ Tags")
                     .GetProperty("relation")
                     .EnumerateArray()
                     .Select(r => r.GetProperty("id").GetString());
        }
        private static IEnumerable<string?> GetSubTagIdsFromJson(JsonElement result)
        {
            return result.GetProperty("properties")
                    .GetProperty("🏷️ SubTags")
                    .GetProperty("relation")
                    .EnumerateArray()
                    .Select(r => r.GetProperty("id").GetString());
        }
    }
}
