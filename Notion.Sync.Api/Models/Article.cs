using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    [Microsoft.EntityFrameworkCore.Index(nameof(Slug), IsUnique = true)]
    [Microsoft.EntityFrameworkCore.Index(nameof(SourcePath), IsUnique = true)]
    public class Article
    {
        [Key]
        public required string Id { get; set; }
        [MaxLength(255)]
        public required string Title { get; set; }
        [MaxLength(255)]
        public required string Slug { get; set; }
        public string? Description { get; set; }
        // Only published articles are stored; Content contains Markdown, not a Notion recordMap.
        public required string Content { get; set; }
        public bool Recommend { get; set; }
        public required string SourcePath { get; set; }
        [MaxLength(64)]
        public required string ContentHash { get; set; }
        public DateTime LastEditedTime { get; set; }
        public ICollection<ArticleTag> ArticleTags { get; set; } = [];
        public ICollection<ArticleSubTag> ArticleSubTags { get; set; } = [];
    }
}
