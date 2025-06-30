using Microsoft.AspNetCore.Mvc;
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
        public NotionController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        [HttpPost("articles/query")]
        public async Task<IActionResult> QueryArticles()
        {
            string notionToken = "";
            string databaseId = "220a52ceaaaf80328f81e1c663be707f";

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
            string notionToken = "";
            string databaseId = "220a52ceaaaf802fad9de344d2b38e23";

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
    }
}
