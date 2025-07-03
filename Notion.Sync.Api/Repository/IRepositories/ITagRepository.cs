using Notion.Sync.Api.Database;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Repository.IRepositories
{
    public interface ITagRepository : IBaseRepository<Tag>
    {
        AppDbContext AppDbContext { get; }
    }
}
