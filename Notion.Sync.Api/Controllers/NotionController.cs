using Microsoft.AspNetCore.Mvc;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Common;
using Notion.Sync.Api.Dtos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Notion.Sync.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotionController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ITagService _tagService;

        public NotionController(HttpClient httpClient, IConfiguration configuration, ITagService tagService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _tagService = tagService;
        }
        [HttpPost("articles/query")]
        public async Task<IActionResult> QueryArticles()
        {
            string notionToken = _configuration["notionToken"];

            string databaseId = "220a52ceaaaf80328f81e1c663be707f";

            Console.WriteLine(_configuration["AllowedHosts"]);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            var url = $"https://api.notion.com/v1/databases/{databaseId}/query";

            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, body);

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");

            List<object> articles = [];

            foreach (var result in results.EnumerateArray())
            {
                Dictionary<string, object> newResult = NotionHelper.FlattenNotionPage(result);
                articles.Add(newResult);
            }

            return Ok(articles);
        }

        [HttpPost("tags/query")]
        public async Task<IActionResult> QueryTags()
        {
            string notionToken = _configuration["notionToken"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            string tagsId = _configuration["NotionDatabaseId:Tags"];
            string subTagsId = _configuration["NotionDatabaseId:SubTags"];

            var subTagsJson = await GetListFromNotionDatabase(subTagsId);

            List<SubTagDto> subTagDtoList = GetSubTagDtoList(subTagsJson);

            var tagsJson = await GetListFromNotionDatabase(tagsId);

            List<TagDto> tagDtoList = GetTagDtoList(tagsJson, subTagDtoList);

            await _tagService.AddTagsWithSubTagsAsync(tagDtoList);

            return Ok();
        }

        private async Task<JsonElement> GetListFromNotionDatabase(string id)
        {
            var url = $"https://api.notion.com/v1/databases/{id}/query";

            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, body);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var jsonResults = doc.RootElement.GetProperty("results");

            return jsonResults;
        }
        private static List<SubTagDto> GetSubTagDtoList(JsonElement subTagsJson)
        {
            List<SubTagDto> subTagDtoList = [];

            foreach (var result in subTagsJson.EnumerateArray())
            {
                var properties = result.GetProperty("properties");
                var slug = properties
                            .GetProperty("Slug")
                            .GetProperty("rich_text")
                            .EnumerateArray()
                            .FirstOrDefault()
                            .GetProperty("plain_text").GetString();
                var title = properties
                            .GetProperty("Name")
                            .GetProperty("title")
                            .EnumerateArray()
                            .FirstOrDefault()
                            .GetProperty("plain_text").GetString();

                var subTagDto = new SubTagDto()
                {
                    NotionId = result.GetProperty("id").GetString(),
                    Title = title,
                    Slug = slug,
                    LastEditedTime = result.GetProperty("last_edited_time").GetDateTime()
                };
                subTagDtoList.Add(subTagDto);
            }

            return subTagDtoList;
        }

        private static List<TagDto> GetTagDtoList(JsonElement tagJson, List<SubTagDto> subTagDtoList)
        {
            List<TagDto> tagDtoList = [];

            var subTagMap = subTagDtoList.ToDictionary(t => t.NotionId!);

            foreach (var result in tagJson.EnumerateArray())
            {
                var properties = result.GetProperty("properties");
                var slug = properties
                            .GetProperty("Slug")
                            .GetProperty("rich_text")
                            .EnumerateArray()
                            .FirstOrDefault()
                            .GetProperty("plain_text").GetString();
                var title = properties.GetProperty("Name")
                            .GetProperty("title")
                            .EnumerateArray()
                            .FirstOrDefault()
                            .GetProperty("plain_text").GetString();
                List<SubTagDto> subTags = properties
                            .GetProperty("🏷️ SubTags")
                            .GetProperty("relation")
                            .EnumerateArray()
                            .Select(r => r.GetProperty("id").GetString())
                            .Where(id => id != null && subTagMap.TryGetValue(id, out _))
                            .Select(id => subTagMap[id!])
                            .ToList();

                var tagDto = new TagDto()
                {
                    NotionId = result.GetProperty("id").GetString(),
                    Title = title,
                    Slug = slug,
                    LastEditedTime = result.GetProperty("last_edited_time").GetDateTime(),
                    SubTags = subTags
                };

                tagDtoList.Add(tagDto);
            }
            return tagDtoList;

        }
    }
}
