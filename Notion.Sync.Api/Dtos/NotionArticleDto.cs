namespace Notion.Sync.Api.Dtos
{
    public class NotionArticleDto
    {
        public string ArticleId { set; get; }
        public string Title { set; get; }
        public bool Published { set; get; } = false;
        public List<string> TagsId { set; get; } = [];
        public List<string> SubTagsId { set; get; } = [];
    }
}
