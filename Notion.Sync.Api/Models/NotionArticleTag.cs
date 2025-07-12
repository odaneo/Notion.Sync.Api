using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notion.Sync.Api.Models
{
    public class NotionArticleTag
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("NotionArticleId")]
        public required string NotionArticleId { get; set; }
        [ForeignKey("TagId")]
        public required string TagId { get; set; }
        public NotionArticle NotionArticle { get; set; } = default!;
        public Tag Tag { get; set; } = default!;
    }
}
