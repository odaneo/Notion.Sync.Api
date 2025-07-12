using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notion.Sync.Api.Models
{
    public class NotionArticleSubTag
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("NotionArticleId")]
        public required string NotionArticleId { get; set; }
        [ForeignKey("SubTagId")]
        public required string SubTagId { get; set; }
        public NotionArticle NotionArticle { get; set; } = default!;
        public SubTag SubTag { get; set; } = default!;
    }
}
