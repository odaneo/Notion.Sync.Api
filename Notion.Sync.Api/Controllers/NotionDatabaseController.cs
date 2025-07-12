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
    public class NotionDatabaseController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly INotionArticleService _notionArticleService;
        private readonly ITagService _tagService;
        private readonly ILogger _logger;
        public NotionDatabaseController(HttpClient httpClient, IConfiguration configuration, ITagService tagService, INotionArticleService notionArticleService, ILogger<NotionDatabaseController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _tagService = tagService;
            _notionArticleService = notionArticleService;
            _logger = logger;
        }
        [HttpPost("articles/queryAll")]
        public async Task<IActionResult> QueryArticles()
        {
            string notionToken = _configuration["notionToken"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            string databaseId = _configuration["NotionDatabaseId:ArticleList"];

            _logger.LogInformation("Start to get articles.");

            var articlesJson = await GetListFromNotionDatabase(databaseId);

            _logger.LogInformation("Successfully retrieved articles from Notion.");

            ICollection<NotionArticleDto> articles = NotionHelper.GetNotionArticleDtoList(articlesJson);

            try
            {
                await _notionArticleService.AddNotionArticleAsync(articles);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to add notion article.");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("tags/queryAll")]
        public async Task<IActionResult> QueryTags()
        {
            string notionToken = _configuration["notionToken"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            string tagsId = _configuration["NotionDatabaseId:Tags"];
            string subTagsId = _configuration["NotionDatabaseId:SubTags"];

            _logger.LogInformation("Start to get sub-tags.");

            var subTagsJson = await GetListFromNotionDatabase(subTagsId);

            _logger.LogInformation("Successfully retrieved sub-tags from Notion.");

            ICollection<SubTagDto> subTagDtoList = NotionHelper.GetSubTagDtoList(subTagsJson);

            _logger.LogInformation("Start to get tags.");

            var tagsJson = await GetListFromNotionDatabase(tagsId);

            _logger.LogInformation("Successfully retrieved tags from Notion.");

            ICollection<TagDto> tagDtoList = NotionHelper.GetTagDtoList(tagsJson, subTagDtoList);

            try
            {
                await _tagService.AddTagsWithSubTagsAsync(tagDtoList);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to add tags with sub-tags.");

                return BadRequest(ex.Message);
            }
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
    }
}
