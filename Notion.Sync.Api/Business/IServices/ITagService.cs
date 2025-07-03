using Notion.Sync.Api.Dtos;

namespace Notion.Sync.Api.Business.IServices
{
    public interface ITagService
    {
        public Task AddTagsWithSubTagsAsync(ICollection<TagDto> tagDtos);
    }
}
