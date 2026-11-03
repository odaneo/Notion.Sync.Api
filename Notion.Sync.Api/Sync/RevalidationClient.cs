using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Notion.Sync.Api.Sync;

public sealed class RevalidationClient(HttpClient http, IOptions<RevalidationOptions> options)
{
    public async Task RefreshAsync(string commitSha, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Url)
        {
            Content = JsonContent.Create(new { commitSha })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new CacheRefreshException($"Cache refresh returned HTTP {(int)response.StatusCode}.");
        try
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (body.RootElement.ValueKind != JsonValueKind.Object ||
                !body.RootElement.TryGetProperty("revalidated", out var revalidated) || revalidated.ValueKind != JsonValueKind.True)
                throw new CacheRefreshException("Cache endpoint did not acknowledge invalidation.");
        }
        catch (JsonException) { throw new CacheRefreshException("Cache endpoint returned an invalid acknowledgement."); }
    }
}
