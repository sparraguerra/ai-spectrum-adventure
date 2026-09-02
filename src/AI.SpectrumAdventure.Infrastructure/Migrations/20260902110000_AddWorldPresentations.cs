using System;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations
{
    [DbContext(typeof(AdventureDbContext))]
    [Migration("20260902110000_AddWorldPresentations")]
    public partial class AddWorldPresentations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                constraints: table => table.PrimaryKey("PK_world_presentations", value => new { value.WorldId, value.LocationId, value.SceneVersion }));
        }

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "world_presentations");

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}