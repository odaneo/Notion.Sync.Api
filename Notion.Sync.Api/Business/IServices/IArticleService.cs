using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Business.IServices
{
    public interface IArticleService
    {
        public Task<Article?> GetArticleByIdNoTracking(string pageId);
        public Task<bool> AddOrUpdateArticleAsync(Article article);
    }
}
