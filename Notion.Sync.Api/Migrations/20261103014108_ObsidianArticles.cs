using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class ObsidianArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF current_schema() NOT IN ('blog', 'blog_test') OR current_schema() IS NULL THEN
                        RAISE EXCEPTION 'Obsidian migration must target blog or blog_test';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_SubTags_Tags_TagId",
                table: "SubTags");

            migrationBuilder.DropTable(
                name: "NotionArticleSubTags");

            migrationBuilder.DropTable(
                name: "NotionArticleTags");

            migrationBuilder.DropTable(
                name: "NotionArticles");

            migrationBuilder.DropIndex(
                name: "IX_SubTags_TagId",
                table: "SubTags");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "SubTags");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Tags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SubTags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubTags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Articles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Articles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedTime",
                table: "Articles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Recommend",
                table: "Articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Articles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourcePath",
                table: "Articles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Articles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ArticleSubTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    SubTagId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleSubTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleSubTags_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleSubTags_SubTags_SubTagId",
                        column: x => x.SubTagId,
                        principalTable: "SubTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleTags_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Repository = table.Column<string>(type: "text", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DatabaseAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CacheInvalidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                    table.CheckConstraint("CK_SyncRuns_CacheAfterDatabase", "\"CacheInvalidatedAt\" IS NULL OR \"DatabaseAppliedAt\" IS NOT NULL");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Slug",
                table: "Tags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubTags_Slug",
                table: "SubTags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Slug",
                table: "Articles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SourcePath",
                table: "Articles",
                column: "SourcePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSubTags_ArticleId_SubTagId",
                table: "ArticleSubTags",
                columns: new[] { "ArticleId", "SubTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSubTags_SubTagId",
                table: "ArticleSubTags",
                column: "SubTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_ArticleId_TagId",
                table: "ArticleTags",
                columns: new[] { "ArticleId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_TagId",
                table: "ArticleTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_Repository_CommitSha",
                table: "SyncRuns",
                columns: new[] { "Repository", "CommitSha" },
                unique: true);

            migrationBuilder.Sql("""
                DO $permissions$
                DECLARE
                    target_schema text := current_schema();
                    writer text := CASE target_schema WHEN 'blog_test' THEN 'blog_test_writer' ELSE 'blog_writer' END;
                    table_name text;
                BEGIN
                    IF EXISTS (SELECT FROM pg_roles WHERE rolname = writer AND
                        (rolsuper OR rolinherit OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls))
                        OR EXISTS (SELECT FROM pg_auth_members m JOIN pg_roles r ON r.oid = m.member WHERE r.rolname = writer)
                    THEN
                        RAISE EXCEPTION 'Existing writer role has unexpected privileges';
                    END IF;
                    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = writer) THEN
                        EXECUTE format('CREATE ROLE %I LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', writer);
                    END IF;
                    EXECUTE format('REVOKE ALL ON SCHEMA %I FROM PUBLIC', target_schema);
                    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I, anon', target_schema, writer);
                    EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I', target_schema, writer);
                    FOREACH table_name IN ARRAY ARRAY['Articles', 'Tags', 'SubTags', 'ArticleTags', 'ArticleSubTags', 'SyncRuns']
                    LOOP
                        EXECUTE format('REVOKE ALL ON %I.%I FROM PUBLIC, anon, authenticated', target_schema, table_name);
                        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON %I.%I TO %I', target_schema, table_name, writer);
                        EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', target_schema, table_name);
                        EXECUTE format('CREATE POLICY writer_access ON %I.%I FOR ALL TO %I USING (true) WITH CHECK (true)', target_schema, table_name, writer);
                        IF table_name <> 'SyncRuns' THEN
                            EXECUTE format('GRANT SELECT ON %I.%I TO anon', target_schema, table_name);
                            EXECUTE format('CREATE POLICY public_read ON %I.%I FOR SELECT TO anon USING (true)', target_schema, table_name);
                        END IF;
                    END LOOP;
                    EXECUTE format('REVOKE ALL ON %I."__EFMigrationsHistory" FROM PUBLIC, anon, authenticated, %I', target_schema, writer);
                    EXECUTE format('ALTER TABLE %I."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY', target_schema);
                END
                $permissions$;
                NOTIFY pgrst, 'reload schema';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    target_schema text := current_schema();
                    writer text := CASE target_schema WHEN 'blog_test' THEN 'blog_test_writer' ELSE 'blog_writer' END;
                BEGIN
                    IF target_schema NOT IN ('blog', 'blog_test') OR target_schema IS NULL THEN
                        RAISE EXCEPTION 'Obsidian rollback must target blog or blog_test';
                    END IF;
                    EXECUTE format('REVOKE ALL ON SCHEMA %I FROM %I, anon', target_schema, writer);
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "ArticleSubTags");

            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: "SyncRuns");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Slug",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_SubTags_Slug",
                table: "SubTags");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Slug",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_SourcePath",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "LastEditedTime",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Recommend",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SourcePath",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Articles");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Tags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SubTags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubTags",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "TagId",
                table: "SubTags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Articles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "NotionArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LastEditedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    Recommend = table.Column<bool>(type: "boolean", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotionArticles_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_SubTags_TagId",
                table: "SubTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticles_ArticleId",
                table: "NotionArticles",
                column: "ArticleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotionArticles_Slug",
                table: "NotionArticles",
                column: "Slug",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_SubTags_Tags_TagId",
                table: "SubTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
