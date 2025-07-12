using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class notionarticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotionArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    LastEditedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotionArticleSubTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotionArticleId = table.Column<string>(type: "text", nullable: false),
                    SubTagId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionArticleSubTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotionArticleSubTags_NotionArticles_NotionArticleId",
                        column: x => x.NotionArticleId,
                        principalTable: "NotionArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotionArticleSubTags_SubTags_SubTagId",
                        column: x => x.SubTagId,
                        principalTable: "SubTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotionArticleTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotionArticleId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionArticleTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotionArticleTags_NotionArticles_NotionArticleId",
                        column: x => x.NotionArticleId,
                        principalTable: "NotionArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotionArticleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticleSubTags_NotionArticleId_SubTagId",
                table: "NotionArticleSubTags",
                columns: new[] { "NotionArticleId", "SubTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticleSubTags_SubTagId",
                table: "NotionArticleSubTags",
                column: "SubTagId");

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticleTags_NotionArticleId_TagId",
                table: "NotionArticleTags",
                columns: new[] { "NotionArticleId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticleTags_TagId",
                table: "NotionArticleTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotionArticleSubTags");

            migrationBuilder.DropTable(
                name: "NotionArticleTags");

            migrationBuilder.DropTable(
                name: "NotionArticles");
        }
    }
}
