using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notion.Sync.Api.Models
{
    public class ArticleSubTag
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("ArticleId")]
        public required string ArticleId { get; set; }
        [ForeignKey("SubTagId")]
        public required string SubTagId { get; set; }
        public Article Article { get; set; } = default!;
        public SubTag SubTag { get; set; } = default!;
    }
}
