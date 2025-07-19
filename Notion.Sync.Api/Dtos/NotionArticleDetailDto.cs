namespace Notion.Sync.Api.Dtos
{
    public class NotionArticleDetailDto
    {
        public string NotionId { get; set; }
        public string ArticleId { get; set; }
        public string Title { get; set; }
        public bool Published { get; set; }
        public DateTime LastEditedTime { get; set; }
        public string ArticleContent { get; set; }

        public List<TagDto> Tags { get; set; } = [];
        public List<SubTagDto> SubTags { get; set; } = [];
    }
}
