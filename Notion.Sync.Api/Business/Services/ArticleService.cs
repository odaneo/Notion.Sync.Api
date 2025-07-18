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
        public async Task<Article?> GetArticleByIdNoTracking(string pageId)
        {
            var result = await _articleRepository.GetByIdNoTrackingAsync(pageId);

            return result;
        }
        public async Task<bool> AddOrUpdateArticleAsync(Article article)
        {
            var existing = await _articleRepository.GetByIdAsync(article.Id);

            if (existing != null)
            {
                existing.Content = article.Content;
            }
            else
            {
                await _articleRepository.AddAsync(article);
            }

            return await _articleRepository.SaveAsync();
        }
    }
}
