using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Extensions.Caching;
using System.Text.Json;

namespace Notion.Sync.Api.Common
{
    public static class SecretsHelper
    {
        private static SecretsManagerCache? _cache;
        public static async Task<string> GetSecretAsync(IConfiguration configuration, string connectionString)
        {
            _cache ??= new SecretsManagerCache(new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName("ap-northeast-1")));

            var secretName = configuration["AWSSecretName"];
            try
            {
                var secretString = await _cache.GetSecretString(secretName);
                var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString)
                            ?? throw new InvalidOperationException("Secret 反序列化失败");

                var username = credentials["username"];
                var password = credentials["password"];

                var newConnectionString = connectionString
                            .Replace("{USERNAME}", username)
                            .Replace("{PASSWORD}", password);

                return newConnectionString;
            }
            catch
            {
                throw;
            }
        }
        public static async Task<string> GetNotionApiToken()
        {
            var cache2 = new SecretsManagerCache(new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName("ap-northeast-1")));
            try
            {
                var secretString = await cache2.GetSecretString("notion-api-token");
                var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString)
                            ?? throw new InvalidOperationException("Secret 反序列化失败");

                var notionToken = credentials["notionToken"];

                return notionToken;
            }
            catch
            {
                throw;
            }
        }
    }
}
