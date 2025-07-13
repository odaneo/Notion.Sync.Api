using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Repository.Repositories
{
    public class ArticleRepository : BaseRepository<Article>, IArticleRepository
    {
        public ArticleRepository(AppDbContext appDbContext) : base(appDbContext)
        {
        }
    }
}
