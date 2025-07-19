using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Repository.IRepositories
{
    public interface INotionArticleRepository : IBaseRepository<NotionArticle>
    {
        AppDbContext AppDbContext { get; }
        Task<NotionArticle?> GetByArticleIdAsyncAsNoTracking(string ArticleId);
        Task<NotionArticle?> GetByIdAsync(string Id);
        new Task<ICollection<NotionArticle>?> GetAllAsync();
    }
}