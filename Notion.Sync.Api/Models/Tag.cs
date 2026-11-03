using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    [Microsoft.EntityFrameworkCore.Index(nameof(Slug), IsUnique = true)]
    public class Tag
    {
        [Key]
        public required string Id { get; set; }
        [MaxLength(255)]
        public required string Title { get; set; }
        [MaxLength(255)]
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? LucideIconName { get; set; }
        public DateTime LastEditedTime { get; set; }
        public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    }
}
