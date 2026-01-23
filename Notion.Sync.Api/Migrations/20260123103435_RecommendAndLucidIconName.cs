using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecommendAndLucidIconName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LucideIconName",
                table: "Tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Recommend",
                table: "NotionArticles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LucideIconName",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Recommend",
                table: "NotionArticles");
        }
    }
}
