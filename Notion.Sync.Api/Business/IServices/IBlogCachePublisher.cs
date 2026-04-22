namespace Notion.Sync.Api.Business.IServices
{
    public interface IBlogCachePublisher
    {
        Task PublishAsync(CancellationToken cancellationToken);
    }
}
