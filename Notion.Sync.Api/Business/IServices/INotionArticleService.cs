using Notion.Sync.Api.Dtos;

namespace Notion.Sync.Api.Business.IServices
{
    public interface INotionArticleService
    {
        public Task AddNotionArticleListAsync(ICollection<NotionArticleDto> notionArticleDtos);
        public Task<List<string>> GetNotionArticleIdListAsync();
    }
}
