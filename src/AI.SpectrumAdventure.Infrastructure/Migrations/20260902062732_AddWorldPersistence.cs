using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_worlds_LegacyGameId",
                table: "worlds",
                column: "LegacyGameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "world_puzzles");

            migrationBuilder.DropTable(
                name: "world_regions");

            migrationBuilder.DropTable(
                name: "worlds");
        }
    }
}
