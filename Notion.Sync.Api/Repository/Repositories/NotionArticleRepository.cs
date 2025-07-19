using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Repository.Repositories
{
    public class NotionArticleRepository : BaseRepository<NotionArticle>, INotionArticleRepository
    {
        public AppDbContext AppDbContext { get; }
        public NotionArticleRepository(AppDbContext appDbContext) : base(appDbContext)
        {
            AppDbContext = appDbContext;
        }
        public async Task<NotionArticle?> GetByArticleIdAsyncAsNoTracking(string ArticleId)
        {
            return await _dbSet
                    .AsNoTracking()
                    .Include(t => t.Article)
                    .Include(t => t.NotionArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(t => t.NotionArticleSubTags)
                        .ThenInclude(at => at.SubTag)
                    .FirstOrDefaultAsync(t => t.ArticleId == ArticleId);
        }
        public async Task<NotionArticle?> GetByIdAsync(string Id)
        {
            return await _dbSet
                    .Include(t => t.Article)
                    .Include(t => t.NotionArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(t => t.NotionArticleSubTags)
                        .ThenInclude(at => at.SubTag)
                    .FirstOrDefaultAsync(t => t.Id == Id);
        }
        public new async Task<ICollection<NotionArticle>?> GetAllAsync()
        {
            return await _dbSet
                   .Include(x => x.Article)
                   .Include(x => x.NotionArticleTags)
                        .ThenInclude(at => at.Tag)
                   .Include(x => x.NotionArticleSubTags)
                        .ThenInclude(at => at.SubTag)
                   .ToListAsync();
        }
    }
}
