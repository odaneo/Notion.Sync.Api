using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNotionArticleSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NotionArticles_Slug",
                table: "NotionArticles",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotionArticles_Slug",
                table: "NotionArticles");
        }
    }
}
