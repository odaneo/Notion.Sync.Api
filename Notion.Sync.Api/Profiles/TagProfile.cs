using AutoMapper;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Profiles
{
    public class TagProfile : Profile
    {
        public TagProfile()
        {
            CreateMap<TagDto, Tag>()
                .ForMember(
                    de => de.Id,
                    opt => opt.MapFrom(src => src.NotionId)
                );
            CreateMap<Tag, TagDto>()
                .ForMember(
                    de => de.NotionId,
                    opt => opt.MapFrom(src => src.Id)
                );
        }
    }
}
