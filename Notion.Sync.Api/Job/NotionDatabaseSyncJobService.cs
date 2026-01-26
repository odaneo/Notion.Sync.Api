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
    public class NotionDatabaseSyncJobService(HttpClient httpClient, IConfiguration configuration, ITagService tagService, INotionArticleService notionArticleService, ILogger<NotionDatabaseSyncJobService> logger)
    {
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

            ICollection<NotionArticleDto> articles = NotionHelper.GetNotionArticleDtoList(articlesJson);

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
                await InvokeLambda();
            }
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
        private async Task<(string Title, string LastEditedTime)> GetTitleAndLastEditedTimeByArticleId(string articleId)
        {
            var url = $"https://api.notion.com/v1/pages/{articleId}";

            var response = await httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string lastEditedTime = root.GetProperty("last_edited_time").GetString()!;

            var titleArray = root.GetProperty("properties")
                    .GetProperty("title")
                    .GetProperty("title")
                    .EnumerateArray();

            string title = string.Join("", titleArray.Select(t => t.GetProperty("plain_text").GetString()));

            return (title, lastEditedTime);
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
