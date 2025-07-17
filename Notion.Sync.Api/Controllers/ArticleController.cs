using Microsoft.AspNetCore.Mvc;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Notion.Sync.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleController(HttpClient httpClient, IConfiguration configuration, IArticleService articleService, ILogger<ArticleController> logger) : ControllerBase
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly IArticleService _articleService = articleService;
        private readonly ILogger _logger = logger;
        [HttpPost("queryAll")]
        public async Task<IActionResult> QueryArticles()
        {
            var pageId = "1b8a52ce-aaaf-803c-8f42-ec4e7e47e8c0";

            string notionToken = _configuration["notionToken"];

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            _logger.LogInformation("Start to get article {pageId}", pageId);

            var content = await FetchBlocksRecursiveAsync(pageId);

            var success = await _articleService.AddOrUpdateArticleAsync(new Article
            {
                Id = pageId,
                Content = content
            });

            if (success)
            {
                _logger.LogInformation("Page {PageId} saved to DB.", pageId);
                return Ok();
            }
            else
            {
                return StatusCode(500, "Failed to fetch from Notion.");
            }
        }
        private async Task<string?> FetchBlocksRecursiveAsync(string pageId)
        {
            var allBlocks = new List<JsonElement>();

            async Task Fetch(string id)
            {
                var url = $"https://api.notion.com/v1/blocks/{id}/children";
                var res = await _httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Error fetching block {id}: {Status}", id, res.StatusCode);
                    return;
                }

                using var stream = await res.Content.ReadAsStreamAsync();
                using var jsonDoc = await JsonDocument.ParseAsync(stream);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("results", out var blocks))
                {
                    foreach (var block in blocks.EnumerateArray())
                    {
                        allBlocks.Add(block.Clone());
                        if (block.TryGetProperty("has_children", out var hasChildrenProp) && hasChildrenProp.GetBoolean())
                        {
                            var childId = block.GetProperty("id").GetString();
                            if (childId != null)
                            {
                                await Fetch(childId);
                            }
                        }
                    }
                }
            }

            await Fetch(pageId);
            return JsonSerializer.Serialize(allBlocks);
        }
    }
}
