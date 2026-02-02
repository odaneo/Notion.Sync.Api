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
        private readonly ISubTagRepository _subTagRepository;

        public TagService(ITagRepository tagRepository, ISubTagRepository subTagRepository, IMapper mapper, ILogger<TagService> logger) : base(mapper, logger)
        {
            _tagRepository = tagRepository;
            _subTagRepository = subTagRepository;
        }
        public async Task AddTagsWithSubTagsAsync(ICollection<TagDto> tagDtos)
        {
            _logger.LogInformation("Start AddTagsWithSubTagsAsync: {Count} tags", tagDtos.Count);

            var tags = _mapper.Map<List<Tag>>(tagDtos);

            var dbContext = _tagRepository.AppDbContext;

            _logger.LogInformation("Begin transaction");

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var tag in tags)
                {

                    var existingTag = await _tagRepository.GetByIdAsync(tag.Id);

                    if (existingTag != null)
                    {
                        existingTag.Title = tag.Title;
                        existingTag.Slug = tag.Slug;
                        existingTag.LastEditedTime = tag.LastEditedTime;
                        existingTag.LucideIconName = tag.LucideIconName;
                        existingTag.Description = tag.Description;
                        _subTagRepository.RemoveRange(existingTag.SubTags);
                        existingTag.SubTags = tag.SubTags;
                    }
                    else
                    {
                        await _tagRepository.AddAsync(tag);
                    }
                }

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
