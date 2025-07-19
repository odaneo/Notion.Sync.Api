using Microsoft.AspNetCore.Mvc;
using Notion.Sync.Api.Business.IServices;

namespace Notion.Sync.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleController(INotionArticleService notionArticleService, ILogger<ArticleController> logger) : ControllerBase
    {
        [HttpGet("{pageId}")]
        public async Task<IActionResult> QueryArticleById(string pageId)
        {
            logger.LogInformation("QueryArticleById called with pageId: {PageId}", pageId);

            try
            {
                var article = await notionArticleService.GetByArticleIdAsync(pageId);

                if (article == null)
                {
                    logger.LogWarning("No article found for pageId: {PageId}", pageId);
                    return NoContent();
                }

                logger.LogInformation("Article retrieved successfully for pageId: {PageId}", pageId);

                return Ok(article);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error querying article by id {PageId}", pageId);

                return BadRequest();
            }
        }
    }
}
