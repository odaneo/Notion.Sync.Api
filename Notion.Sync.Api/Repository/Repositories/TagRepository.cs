using Microsoft.EntityFrameworkCore;
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
        public async Task<Tag?> GetByIdAsync(string Id)
        {
            return await _dbSet.Include(t => t.SubTags).FirstOrDefaultAsync(t => t.Id == Id);
        }
    }
}
