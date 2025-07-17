using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Business.IServices
{
    public interface IArticleService
    {
        public Task<bool> AddOrUpdateArticleAsync(Article article);
    }
}
