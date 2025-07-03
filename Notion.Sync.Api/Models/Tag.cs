using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    public class Tag
    {
        [Key]
        public required string Id { get; set; }
        [MaxLength(255)]
        public string? Title { get; set; }
        [MaxLength(255)]
        public string? Slug { get; set; }
        public DateTime LastEditedTime { get; set; }
        public List<SubTag> SubTags { get; set; } = [];
    }
}
