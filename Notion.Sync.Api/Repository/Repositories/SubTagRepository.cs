using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Repository.Repositories
{
    public class SubTagRepository : BaseRepository<SubTag>, ISubTagRepository
    {
        public SubTagRepository(AppDbContext appDbContext) : base(appDbContext)
        {
        }
    }
}
