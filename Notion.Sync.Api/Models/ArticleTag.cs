using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notion.Sync.Api.Models
{
    public class ArticleTag
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("ArticleId")]
        public required string ArticleId { get; set; }
        [ForeignKey("TagId")]
        public required string TagId { get; set; }
        public Article Article { get; set; } = default!;
        public Tag Tag { get; set; } = default!;
    }
}
