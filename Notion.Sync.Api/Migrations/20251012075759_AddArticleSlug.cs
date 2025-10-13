﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "NotionArticles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "NotionArticles");
        }
    }
}
