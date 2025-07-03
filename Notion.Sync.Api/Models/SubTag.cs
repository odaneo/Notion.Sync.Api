using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    public class SubTag
    {
        [Key]
        public required string Id { get; set; }
        [MaxLength(255)]
        public string? Title { get; set; }
        [MaxLength(255)]
        public string? Slug { get; set; }
        public DateTime LastEditedTime { get; set; }
        public string TagId { get; set; } = default!;
        public Tag Tag { get; set; } = default!;
    }
}
