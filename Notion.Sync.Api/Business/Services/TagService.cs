using AutoMapper;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Repository.IRepositories;

namespace Notion.Sync.Api.Business.Services
{
    public class TagService : BaseService<TagService>, ITagService
    {
        private readonly ITagRepository _tagRepository;
        public TagService(ITagRepository tagRepository, IMapper mapper, ILogger<TagService> logger) : base(mapper, logger)
        {
            _tagRepository = tagRepository;
        }
        public async Task AddTagsWithSubTagsAsync(ICollection<TagDto> tagDtos)
        {
            _logger.LogInformation("Start AddTagsWithSubTagsAsync: {Count} tags", tagDtos.Count);

            var tags = _mapper.Map<List<Tag>>(tagDtos);

            _logger.LogDebug("Mapped {Count} tags with SubTags", tags.Count);

            var dbContext = _tagRepository.AppDbContext;

            _logger.LogInformation("Begin transaction");

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                await _tagRepository.AddRangeAsync(tags);

                var success = await _tagRepository.SaveAsync();
                if (success)
                {
                    _logger.LogInformation("Successfully saved {Count} tags with SubTags", tags.Count);
                }
                else
                {
                    _logger.LogWarning("No Tag entities were saved to the database.");
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Transaction end");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving tags with sub-tags. Rolling back transaction.");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
