using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.SpectrumAdventure.Infrastructure.Migrations;

public partial class AddPlaytestSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            constraints: table => table.PrimaryKey("PK_playtest_sessions", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_playtest_sessions_GameId", table: "playtest_sessions", column: "GameId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "playtest_sessions");
}