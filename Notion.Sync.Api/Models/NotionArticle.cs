using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notion.Sync.Api.Models
{
    public class NotionArticle
    {
        [Key]
        public required string Id { get; set; }
        [ForeignKey("ArticleId")]
        public string ArticleId { set; get; }
        public Article Article { get; set; }
        [MaxLength(255)]
        public string Title { set; get; }
        public bool Published { set; get; } = false;
        public DateTime LastEditedTime { get; set; }
        public ICollection<NotionArticleSubTag> NotionArticleSubTags { get; set; } = [];
        public ICollection<NotionArticleTag> NotionArticleTags { get; set; } = [];
    }
}
