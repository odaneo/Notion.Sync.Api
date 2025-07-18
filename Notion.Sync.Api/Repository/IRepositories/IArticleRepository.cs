using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Repository.IRepositories
{
    public interface IArticleRepository : IBaseRepository<Article>
    {
        public Task<Article?> GetByIdAsync(string Id);
        public Task<Article?> GetByIdNoTrackingAsync(string Id);
    }
}
