using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Business.IServices
{
    public interface INotionArticleService
    {
        Task<NotionArticle?> GetByArticleIdAsync(string ArticleId);
        Task AddNotionArticleListAsync(ICollection<NotionArticleDto> notionArticleDtos);
        Task<List<string>> GetNotionArticleIdListAsync();
    }
}
