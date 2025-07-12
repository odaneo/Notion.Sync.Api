using AutoMapper;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Business.Services
{
    public class NotionArticleService : BaseService<NotionArticle>, INotionArticleService
    {
        private readonly INotionArticleRepository _notionArticleRepository;
        public NotionArticleService(IMapper mapper, ILogger<NotionArticle> logger, INotionArticleRepository notionArticleRepository) : base(mapper, logger)
        {
            _notionArticleRepository = notionArticleRepository;
        }
        public Task AddNotionArticleAsync(ICollection<NotionArticleDto> notionArticleDtos)
        {
            //
        }
    }
}
