using AutoMapper;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Business.Services
{
    public class ArticleService : BaseService<Article>, IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        public ArticleService(IMapper mapper, ILogger<Article> logger, IArticleRepository articleRepository) : base(mapper, logger)
        {
            _articleRepository = articleRepository;
        }
    }
}
