using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Options;

namespace Notion.Sync.Api.Business.Services
{
    public class CloudflareKvClient : ICloudflareKvClient
    {
        private readonly HttpClient _httpClient;
        private readonly CloudflareKvOptions _options;

        public CloudflareKvClient(HttpClient httpClient, IOptions<CloudflareKvOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new InvalidOperationException("CloudflareKv:AccountId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.NamespaceId))
            {
                throw new InvalidOperationException("CloudflareKv:NamespaceId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                throw new InvalidOperationException("CloudflareKv:ApiToken is not configured.");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        }

        public Task PutJsonAsync(string key, string json, int? expirationTtlSeconds, CancellationToken cancellationToken)
        {
            return PutAsync(key, json, "application/json", expirationTtlSeconds, cancellationToken);
        }

        public Task PutTextAsync(string key, string value, CancellationToken cancellationToken)
        {
            return PutAsync(key, value, "text/plain", null, cancellationToken);
        }

        private async Task PutAsync(
            string key,
            string value,
            string contentType,
            int? expirationTtlSeconds,
            CancellationToken cancellationToken)
        {
            var url = BuildValueUrl(key, expirationTtlSeconds);
            using var content = new StringContent(value, Encoding.UTF8, contentType);
            using var response = await _httpClient.PutAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Cloudflare KV write failed for key {key}. Status: {(int)response.StatusCode}. Body: {body}"
            );
        }

        private string BuildValueUrl(string key, int? expirationTtlSeconds)
        {
            var url =
                $"https://api.cloudflare.com/client/v4/accounts/{_options.AccountId}/storage/kv/namespaces/{_options.NamespaceId}/values/{Uri.EscapeDataString(key)}";

            return expirationTtlSeconds.HasValue
                ? $"{url}?expiration_ttl={expirationTtlSeconds.Value}"
                : url;
        }
    }
}
