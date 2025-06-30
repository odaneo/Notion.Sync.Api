using System.Text.Json;

namespace Notion.Sync.Api.Common
{
    public static class NotionHelper
    {
        public static Dictionary<string, object> FlattenNotionPage(JsonElement result)
        {
            Dictionary<string, object> newResult = [];

            var properties = result.GetProperty("properties");
            foreach (var prop in properties.EnumerateObject())
            {
                string key = prop.Name;
                var value = prop.Value;

                string type = value.GetProperty("type").GetString();

                switch (type)
                {
                    case "title":
                        var titleArr = value.GetProperty("title").EnumerateArray();
                        var mention = titleArr.FirstOrDefault(v => v.GetProperty("type").GetString() == "mention");
                        var text = titleArr.FirstOrDefault(v => v.GetProperty("type").GetString() == "text");

                        if (mention.ValueKind != JsonValueKind.Undefined)
                        {
                            var articleId = mention.GetProperty("mention").GetProperty("page").GetProperty("id").GetString();
                            var title = mention.GetProperty("plain_text").GetString();
                            newResult.Add("ArticleId", articleId);
                            newResult.Add("Title", title);
                        }
                        else if (text.ValueKind != JsonValueKind.Undefined)
                        {
                            var name = text.GetProperty("plain_text").GetString();
                            newResult.Add("Title", name);
                        }
                        else
                        {
                            newResult.Add("Title", null);
                        }
                        break;
                    case "rich_text":
                        var l2 = value.GetProperty("rich_text").EnumerateArray().Select(t => t.GetProperty("plain_text").GetString());
                        newResult.Add(key, string.Join("", l2));
                        break;
                    case "relation":
                        var l3 = value.GetProperty("relation")
                            .EnumerateArray()
                            .Select(r => r.GetProperty("id").GetString())
                            .ToList();
                        newResult.Add(key, l3);
                        break;
                    case "select":
                        var l4 = value.GetProperty("select").GetProperty("name").GetString();
                        newResult.Add(key, l4);
                        break;
                    default:
                        newResult.Add(key, null);
                        break;
                }
            }

            newResult.Add("NotionId", result.GetProperty("id").GetString());
            newResult.Add("LastEditedTime", result.GetProperty("last_edited_time").GetDateTime());

            return newResult;
        }
    }
}
