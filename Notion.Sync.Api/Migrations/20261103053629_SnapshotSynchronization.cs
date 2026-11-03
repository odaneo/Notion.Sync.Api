using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notion.Sync.Api.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresRevalidation",
                table: "SyncRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "WorkflowRunNumber",
                table: "SyncRuns",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresRevalidation",
                table: "SyncRuns");

            migrationBuilder.DropColumn(
                name: "WorkflowRunNumber",
                table: "SyncRuns");
        }
    }
}
