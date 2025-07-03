using Microsoft.AspNetCore.Mvc;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Common;
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

            var tags = await GetListFromNotionDatabase(tagsId);

            var subTags = await GetListFromNotionDatabase(subTagsId);

            return Ok(tags);
        }

        private async Task<List<object>> GetListFromNotionDatabase(string id)
        {
            var url = $"https://api.notion.com/v1/databases/{id}/query";

            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, body);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var jsonResults = doc.RootElement.GetProperty("results");

            List<object> tags = [];

            foreach (var result in jsonResults.EnumerateArray())
            {
                Dictionary<string, object> newResult = NotionHelper.FlattenNotionPage(result);
                tags.Add(newResult);
            }

            return tags;
        }
    }
}
