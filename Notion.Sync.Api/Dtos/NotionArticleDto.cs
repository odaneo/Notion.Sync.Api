namespace Notion.Sync.Api.Dtos
{
    public class NotionArticleDto
    {
        public string NotionId { get; set; }
        public string ArticleId { set; get; }
        public string Title { set; get; }
        public bool Published { set; get; } = false;
        public DateTime LastEditedTime { get; set; }
        public ICollection<string> TagsId { set; get; } = [];
        public ICollection<string> SubTagsId { set; get; } = [];
    }
}
