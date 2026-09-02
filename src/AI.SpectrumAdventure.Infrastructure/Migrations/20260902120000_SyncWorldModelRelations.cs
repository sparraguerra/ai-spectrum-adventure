using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncWorldModelRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_world_lore_WorldId",
                table: "world_lore",
                column: "WorldId");

            migrationBuilder.AddForeignKey(
                name: "FK_world_lore_worlds_WorldId",
                table: "world_lore",
                column: "WorldId",
                principalTable: "worlds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_world_lore_worlds_WorldId",
                table: "world_lore");

            migrationBuilder.DropIndex(
                name: "IX_world_lore_WorldId",
                table: "world_lore");
        }
    }
}