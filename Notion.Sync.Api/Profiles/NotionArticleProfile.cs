using AutoMapper;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Profiles
{
    public class NotionArticleProfile : Profile
    {
        public NotionArticleProfile()
        {
            CreateMap<NotionArticleDto, NotionArticle>()
                .ForMember(
                    de => de.Id,
                    opt => opt.MapFrom(src => src.NotionId)
                )
                .AfterMap((src, dest) =>
                {
                    dest.NotionArticleTags = src.TagsIds?.Select(tagId => new NotionArticleTag
                    {
                        TagId = tagId,
                        NotionArticleId = src.NotionId
                    }).ToList() ?? [];

                    dest.NotionArticleSubTags = src.SubTagsIds?.Select(subTagId => new NotionArticleSubTag
                    {
                        SubTagId = subTagId,
                        NotionArticleId = src.NotionId
                    }).ToList() ?? [];
                });
            CreateMap<NotionArticle, NotionArticleDetailDto>()
                .ForMember(dest => dest.NotionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ArticleContent, opt => opt.MapFrom(src => src.Article.Content))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.NotionArticleTags.Select(nt => nt.Tag)))
                .ForMember(dest => dest.SubTags, opt => opt.MapFrom(src => src.NotionArticleSubTags.Select(st => st.SubTag)));
        }
    }
}
