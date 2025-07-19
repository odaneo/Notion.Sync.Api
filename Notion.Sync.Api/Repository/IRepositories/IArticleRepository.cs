using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Repository.IRepositories
{
    public interface IArticleRepository : IBaseRepository<Article>
    {
        Task<Article?> GetByIdAsync(string Id);
        Task<Article?> GetByIdNoTrackingAsync(string Id);
    }
}
