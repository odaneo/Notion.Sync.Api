namespace Notion.Sync.Api.Dtos
{
    public class NotionArticleDto
    {
        public string NotionId { get; set; }
        public string ArticleId { set; get; }
        public string Title { set; get; }
        public string Slug { set; get; }
        public string Description { get; set; }
        public bool Published { set; get; } = false;
        public bool Recommend { set; get; } = false;
        public DateTime LastEditedTime { get; set; }
        public ICollection<string> TagsIds { set; get; } = [];
        public ICollection<string> SubTagsIds { set; get; } = [];
    }
}
