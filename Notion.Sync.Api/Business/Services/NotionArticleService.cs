using AutoMapper;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Common;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Business.Services
{
    public class NotionArticleService : BaseService<NotionArticle>, INotionArticleService
    {
        private readonly INotionArticleRepository _notionArticleRepository;
        private readonly IArticleRepository _articleRepository;
        public NotionArticleService(IMapper mapper, ILogger<NotionArticle> logger, INotionArticleRepository notionArticleRepository, IArticleRepository articleRepository) : base(mapper, logger)
        {
            _notionArticleRepository = notionArticleRepository;
            _articleRepository = articleRepository;
        }
        public async Task<List<string>> GetNotionArticleIdListAsync()
        {
            var notionArticleIdList = await _notionArticleRepository.GetAllAsync();

            return notionArticleIdList.Select(x => x.ArticleId).ToList();
        }
        public async Task AddNotionArticleListAsync(ICollection<NotionArticleDto> notionArticleDtos)
        {
            _logger.LogInformation("Start AddNotionArticleAsync: {Count}", notionArticleDtos.Count);

            var notionArticles = _mapper.Map<List<NotionArticle>>(notionArticleDtos);

            var existingArticles = await _notionArticleRepository.GetAllAsync();

            var toRemove = existingArticles?.Where(x => !notionArticles.Select(a => a.Id).Contains(x.Id)).ToList();

            var dbContext = _notionArticleRepository.AppDbContext;

            _logger.LogInformation("Begin transaction");

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                if (toRemove != null && toRemove.Any())
                {
                    foreach (var toRemoveArticle in toRemove)
                    {
                        if (toRemoveArticle.Article != null)
                        {
                            _articleRepository.Remove(toRemoveArticle.Article);
                        }
                    }

                    _notionArticleRepository.RemoveRange(toRemove);
                }

                foreach (var article in notionArticles)
                {
                    var existing = await _notionArticleRepository.GetByIdAsync(article.Id);

                    if (existing != null)
                    {
                        existing.Title = article.Title;
                        existing.Published = article.Published;
                        existing.LastEditedTime = article.LastEditedTime;

                        if (existing.Article == null)
                        {
                            var newArticle = new Article
                            {
                                Id = existing.ArticleId,
                                Content = null
                            };
                            existing.Article = newArticle;
                        }

                        ManyToManySyncHelper.SyncManyToMany(
                            existing.NotionArticleTags,
                            article.NotionArticleTags.Select(x => x.TagId),
                            x => x.TagId,
                            tagId => new NotionArticleTag { NotionArticleId = existing.Id, TagId = tagId }
                        );
                        ManyToManySyncHelper.SyncManyToMany(
                            existing.NotionArticleSubTags,
                            article.NotionArticleSubTags.Select(x => x.SubTagId),
                            x => x.SubTagId,
                            subTagId => new NotionArticleSubTag { NotionArticleId = existing.Id, SubTagId = subTagId }
                        );
                    }
                    else
                    {
                        var newArticle = new Article
                        {
                            Id = article.ArticleId,
                            Content = null
                        };
                        article.Article = newArticle;
                        await _notionArticleRepository.AddAsync(article);
                    }
                }

                var orphanArticles = dbContext.Articles
                       .Where(a => !dbContext.NotionArticles.Any(n => n.ArticleId == a.Id))
                       .ToList();

                if (orphanArticles.Any())
                {
                    _articleRepository.RemoveRange(orphanArticles);
                    _logger.LogInformation("Removed {Count} orphan articles.", orphanArticles.Count);
                }

                var success = await _notionArticleRepository.SaveAsync();

                if (success)
                {
                    _logger.LogInformation("Successfully saved {Count} notion articles", notionArticles.Count);
                }
                else
                {
                    _logger.LogWarning("No notion articles entities were saved to the database.");
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Transaction end");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving notion articles. Rolling back transaction.");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
