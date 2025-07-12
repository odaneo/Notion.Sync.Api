using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Repository.IRepositories
{
    public interface INotionArticleRepository: IBaseRepository<NotionArticle>
    {
        Task<NotionArticle?> GetByIdAsync(string Id);
    }
}