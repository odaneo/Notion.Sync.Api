using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Common;
using Notion.Sync.Api.Dtos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Notion.Sync.Api.Job
{
    public class NotionDatabaseSyncJobService(HttpClient httpClient, IConfiguration configuration, ITagService tagService, INotionArticleService notionArticleService, IBlogCachePublisher blogCachePublisher, ILogger<NotionDatabaseSyncJobService> logger)
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public async Task SyncTagsAndArticleListAsync()
        {
            string notionToken;
            bool isDev = configuration["ASPNETCORE_ENVIRONMENT"] == Environments.Development;

            // SyncTagsAndSubTags
            if (isDev)
            {
                notionToken = configuration["NotionTokenLocal"]!;
            }
            else
            {
                notionToken = configuration["NotionToken:notionToken"]
                      ?? throw new InvalidOperationException("NotionToken:notionToken 未加载");
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionToken);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            string tagsId = configuration["NotionDatabaseId:Tags"];
            string subTagsId = configuration["NotionDatabaseId:SubTags"];

            logger.LogInformation("Start to get sub-tags.");

            var subTagsJson = await GetListFromNotionDatabase(subTagsId);

            logger.LogInformation("Successfully retrieved sub-tags from Notion.");

            ICollection<SubTagDto> subTagDtoList = NotionHelper.GetSubTagDtoList(subTagsJson);

            logger.LogInformation("Start to get tags.");

            var tagsJson = await GetListFromNotionDatabase(tagsId);

            logger.LogInformation("Successfully retrieved tags from Notion.");

            ICollection<TagDto> tagDtoList = NotionHelper.GetTagDtoList(tagsJson, subTagDtoList);

            try
            {
                await tagService.AddTagsWithSubTagsAsync(tagDtoList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, "Failed to add tags with sub-tags.");

                throw new Exception("Failed to save tags and sub-tags");
            }

            //SyncArticleList
            string databaseId = configuration["NotionDatabaseId:ArticleList"];

            logger.LogInformation("Start to get articles.");

            var articlesJson = await GetListFromNotionDatabase(databaseId);

            logger.LogInformation("Successfully retrieved articles from Notion.");

            ICollection<NotionArticleDto> articles = NotionHelper.GetNotionArticleDtoList(articlesJson, GetTitleAndLastEditedTimeByArticleId);

            try
            {
                await notionArticleService.AddNotionArticleListAsync(articles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, "Failed to add notion article.");

                throw new Exception("Failed to article list");
            }
            if (!isDev)
            {
                await InvokeNodejs();
            }

            logger.LogInformation("Publishing blog cache to Cloudflare KV.");
            await blogCachePublisher.PublishAsync(CancellationToken.None);
            logger.LogInformation("Cloudflare KV blog cache published successfully.");

            //if (!isDev)
            //{
            //    await InvokeLambda();
            //}
        }
        private async Task<JsonElement> GetListFromNotionDatabase(string id)
        {
            var url = $"https://api.notion.com/v1/databases/{id}/query";

            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, body);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var jsonResults = doc.RootElement.GetProperty("results");

            return jsonResults;
        }
        private async Task<(bool Found, string Title, DateTime LastEditedTime)> GetTitleAndLastEditedTimeByArticleId(string articleId)
        {
            try
            {
                var url = $"https://api.notion.com/v1/pages/{articleId}";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!response.IsSuccessStatusCode
                    && response.StatusCode == System.Net.HttpStatusCode.NotFound
                    && root.TryGetProperty("code", out var code)
                    && code.GetString() == "object_not_found")
                {
                    logger.LogWarning("Skip articleId {ArticleId} because the referenced Notion page was not found or not shared with the integration.", articleId);
                    return (false, "", default);
                }

                DateTime lastEditedTime = root.GetProperty("last_edited_time").GetDateTime()!;

                var titleArray = root.GetProperty("properties")
                        .GetProperty("title")
                        .GetProperty("title")
                        .EnumerateArray();

                string title = string.Join("", titleArray.Select(t => t.GetProperty("plain_text").GetString()));

                return (true, title, lastEditedTime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting title and last edited time for articleId {ArticleId}", articleId);
                return (false, "", default);
            }
        }
        private async Task InvokeNodejs()
        {
            string url = "http://localhost:3000/update_notion_articles";

            try
            {
                logger.LogInformation("Sending request to Node.js sync service...");

                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string jsonResult = await response.Content.ReadAsStringAsync();

                logger.LogInformation($"Sync task finished successfully. Result: {jsonResult}");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP request to Node.js sync service failed.");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Node.js sync service request timed out.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while invoking Node.js sync service.");
                throw;
            }
        }
        private async Task InvokeLambda()
        {
            var config = new AmazonLambdaConfig
            {
                RegionEndpoint = RegionEndpoint.APNortheast1
            };
            var functionName = "updateNotionArticles";
            using var client = new AmazonLambdaClient(config);

            try
            {
                logger.LogInformation("Invoking Lambda function {FunctionName}", functionName);

                var resp = await client.InvokeAsync(new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                });

                logger.LogInformation("Lambda invoke completed with HTTP {StatusCode}, FunctionError={FunctionError}",
                  resp.StatusCode, resp.FunctionError ?? "None");

                string body;
                using (var reader = new StreamReader(resp.Payload, Encoding.UTF8))
                    body = await reader.ReadToEndAsync();

                logger.LogInformation("Lambda response payload: {Payload}", body);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error invoking Lambda");
                throw;
            }
        }
    }
}
