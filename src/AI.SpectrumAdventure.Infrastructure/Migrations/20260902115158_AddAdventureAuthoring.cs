using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdventureAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdventureVersionId",
                table: "games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "adventure_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureIdentifier = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartingLocationId = table.Column<string>(type: "text", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidationJson = table.Column<string>(type: "jsonb", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adventure_drafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "adventure_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureIdentifier = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adventure_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "authoring_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authoring_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "authoring_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    RequestSummary = table.Column<string>(type: "text", nullable: false),
                    PatchJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidationJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authoring_proposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_games_AdventureVersionId",
                table: "games",
                column: "AdventureVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_adventure_drafts_AdventureIdentifier",
                table: "adventure_drafts",
                column: "AdventureIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adventure_versions_AdventureIdentifier_Sequence",
                table: "adventure_versions",
                columns: new[] { "AdventureIdentifier", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_adventure_versions_DraftId_Sequence",
                table: "adventure_versions",
                columns: new[] { "DraftId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_authoring_audit_entries_DraftId_OccurredAt",
                table: "authoring_audit_entries",
                columns: new[] { "DraftId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adventure_drafts");

            migrationBuilder.DropTable(
                name: "adventure_versions");

            migrationBuilder.DropTable(
                name: "authoring_audit_entries");

            migrationBuilder.DropTable(
                name: "authoring_proposals");

            migrationBuilder.DropIndex(
                name: "IX_games_AdventureVersionId",
                table: "games");

            migrationBuilder.DropColumn(
                name: "AdventureVersionId",
                table: "games");
        }
    }
}
