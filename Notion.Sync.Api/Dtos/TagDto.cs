namespace Notion.Sync.Api.Dtos
{
    public class TagDto
    {
        public string NotionId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string LucideIconName { get; set; }
        public DateTime LastEditedTime { get; set; }
        public ICollection<SubTagDto> SubTags { get; set; } = [];
    }
}
