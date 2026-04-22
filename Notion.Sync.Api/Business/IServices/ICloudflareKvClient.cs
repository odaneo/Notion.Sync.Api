namespace Notion.Sync.Api.Business.IServices
{
    public interface ICloudflareKvClient
    {
        Task PutJsonAsync(string key, string json, int? expirationTtlSeconds, CancellationToken cancellationToken);
        Task PutTextAsync(string key, string value, CancellationToken cancellationToken);
    }
}
