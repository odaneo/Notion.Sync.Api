using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class Description : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "NotionArticles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "NotionArticles");
        }
    }
}
