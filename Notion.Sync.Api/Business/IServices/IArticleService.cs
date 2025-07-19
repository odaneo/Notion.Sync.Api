using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Business.IServices
{
    public interface IArticleService
    {
        Task<Article?> GetArticleByIdNoTracking(string pageId);
        Task<bool> AddOrUpdateArticleAsync(Article article);
    }
}
