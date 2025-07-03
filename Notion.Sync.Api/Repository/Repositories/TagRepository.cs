using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Repository.Repositories
{
    public class TagRepository : BaseRepository<Tag>, ITagRepository
    {
        public AppDbContext AppDbContext { get; }
        public TagRepository(AppDbContext appDbContext) : base(appDbContext)
        {
            AppDbContext = appDbContext;
        }
    }
}
