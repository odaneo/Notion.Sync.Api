using System.ComponentModel.DataAnnotations;

namespace Notion.Sync.Api.Models
{
    public class Article
    {
        [Key]
        public string Id { get; set; }
        public string? Content { get; set; }
        public NotionArticle NotionArticle { get; set; }
    }
}
