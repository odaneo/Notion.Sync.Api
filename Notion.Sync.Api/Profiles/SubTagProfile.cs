using AutoMapper;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Profiles
{
    public class SubTagProfile : Profile
    {
        public SubTagProfile()
        {
            CreateMap<SubTagDto, SubTag>()
                .ForMember(
                    de => de.Id,
                    opt => opt.MapFrom(src => src.NotionId)
                );
        }
    }
}
