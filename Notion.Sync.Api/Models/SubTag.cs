using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    [Microsoft.EntityFrameworkCore.Index(nameof(Slug), IsUnique = true)]
    public class SubTag
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [MaxLength(255)]
        public required string Title { get; set; }
        [MaxLength(255)]
        public required string Slug { get; set; }
        public DateTime LastEditedTime { get; set; }
        public ICollection<ArticleSubTag> ArticleSubTags { get; set; } = [];
    }
}
