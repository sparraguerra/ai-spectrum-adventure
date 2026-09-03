using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "adventures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adventures", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Json = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "playtest_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftRevision = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotHash = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceVersionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playtest_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "visual_assets",
                columns: table => new
                {
                    SceneStateKey = table.Column<string>(type: "text", nullable: false),
                    BlobUri = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visual_assets", x => x.SceneStateKey);
                });

            migrationBuilder.CreateTable(
                name: "world_npc_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_npc_states", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "world_presentations",
                columns: table => new
                {
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<string>(type: "text", nullable: false),
                    SceneVersion = table.Column<int>(type: "integer", nullable: false),
                    FactualName = table.Column<string>(type: "text", nullable: false),
                    FactualDescription = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Atmosphere = table.Column<string>(type: "text", nullable: true),
                    VisualCharacteristicsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_presentations", x => new { x.WorldId, x.LocationId, x.SceneVersion });
                });

            migrationBuilder.CreateTable(
                name: "world_puzzles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_puzzles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "worlds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyGameId = table.Column<Guid>(type: "uuid", nullable: true),
                    Seed = table.Column<string>(type: "text", nullable: false),
                    GenerationVersion = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worlds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "world_connections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLocationId = table.Column<string>(type: "text", nullable: false),
                    DestinationLocationId = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Availability = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_connections", x => new { x.WorldId, x.Id });
                    table.ForeignKey(
                        name: "FK_world_connections_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Cause = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_world_events_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_generation_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationKey = table.Column<string>(type: "text", nullable: false),
                    WorldSeed = table.Column<string>(type: "text", nullable: false),
                    GenerationVersion = table.Column<string>(type: "text", nullable: false),
                    SourceLocationId = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    ExpansionOrdinal = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_generation_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_world_generation_metadata_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_locations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StructuralNameKey = table.Column<string>(type: "text", nullable: false),
                    BaseDescription = table.Column<string>(type: "text", nullable: false),
                    EnvironmentJson = table.Column<string>(type: "jsonb", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SceneVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_locations", x => new { x.WorldId, x.Id });
                    table.ForeignKey(
                        name: "FK_world_locations_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_lore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_lore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_world_lore_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_regions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TerrainProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    MaximumExpansions = table.Column<int>(type: "integer", nullable: false),
                    AllowedDirectionsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_regions", x => new { x.WorldId, x.Id });
                    table.ForeignKey(
                        name: "FK_world_regions_worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_games_AdventureVersionId",
                table: "games",
                column: "AdventureVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_playtest_sessions_GameId",
                table: "playtest_sessions",
                column: "GameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_world_connections_WorldId_SourceLocationId_Direction",
                table: "world_connections",
                columns: new[] { "WorldId", "SourceLocationId", "Direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_world_events_WorldId_Sequence",
                table: "world_events",
                columns: new[] { "WorldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_world_generation_metadata_WorldId_GenerationKey",
                table: "world_generation_metadata",
                columns: new[] { "WorldId", "GenerationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_world_lore_WorldId",
                table: "world_lore",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_worlds_LegacyGameId",
                table: "worlds",
                column: "LegacyGameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adventure_drafts");

            migrationBuilder.DropTable(
                name: "adventure_versions");

            migrationBuilder.DropTable(
                name: "adventures");

            migrationBuilder.DropTable(
                name: "authoring_audit_entries");

            migrationBuilder.DropTable(
                name: "authoring_proposals");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "playtest_sessions");

            migrationBuilder.DropTable(
                name: "visual_assets");

            migrationBuilder.DropTable(
                name: "world_connections");

            migrationBuilder.DropTable(
                name: "world_events");

            migrationBuilder.DropTable(
                name: "world_generation_metadata");

            migrationBuilder.DropTable(
                name: "world_locations");

            migrationBuilder.DropTable(
                name: "world_lore");

            migrationBuilder.DropTable(
                name: "world_npc_states");

            migrationBuilder.DropTable(
                name: "world_presentations");

            migrationBuilder.DropTable(
                name: "world_puzzles");

            migrationBuilder.DropTable(
                name: "world_regions");

            migrationBuilder.DropTable(
                name: "worlds");
        }
    }
}
