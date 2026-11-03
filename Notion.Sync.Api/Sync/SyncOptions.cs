namespace Notion.Sync.Api.Sync;

public sealed class SyncOptions
{
    public string Repository { get; init; } = "";
    public string Branch { get; init; } = "";
    public string Token { get; init; } = "";
}

public sealed class RevalidationOptions
{
    public string Url { get; init; } = "";
    public string Token { get; init; } = "";
}
