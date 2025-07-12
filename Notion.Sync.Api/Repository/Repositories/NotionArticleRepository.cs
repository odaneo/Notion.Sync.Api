using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Repository.Repositories
{
    public class NotionArticleRepository : BaseRepository<NotionArticle>, INotionArticleRepository
    {
        public NotionArticleRepository(AppDbContext appDbContext) : base(appDbContext)
        {
        }
        public async Task<NotionArticle?> GetByIdAsync(string Id)
        {
            return await _dbSet
                    //.Include(t => t.NotionArticleTags)
                    //    .ThenInclude(at => at.Tag)
                    //.Include(t => t.NotionArticleSubTags)
                    //    .ThenInclude(at => at.SubTag)
                    .FirstOrDefaultAsync(t => t.Id == Id);
        }
    }
}
