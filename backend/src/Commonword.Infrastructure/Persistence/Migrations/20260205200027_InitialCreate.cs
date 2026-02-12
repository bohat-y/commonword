using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commonword.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "puzzles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_daily = table.Column<bool>(type: "boolean", nullable: false),
                    data = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puzzles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solve_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    puzzle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solve_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telemetry_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    client = table.Column<string>(type: "text", nullable: false),
                    player_id = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entries",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    col = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "char(1)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entries", x => new { x.session_id, x.row, x.col });
                    table.ForeignKey(
                        name: "FK_entries_solve_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "solve_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_puzzles_imported_at",
                table: "puzzles",
                column: "imported_at");

            migrationBuilder.CreateIndex(
                name: "IX_puzzles_is_daily",
                table: "puzzles",
                column: "is_daily");

            migrationBuilder.CreateIndex(
                name: "IX_solve_sessions_player_id",
                table: "solve_sessions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_solve_sessions_puzzle_id",
                table: "solve_sessions",
                column: "puzzle_id");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_events_occurred_at",
                table: "telemetry_events",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entries");

            migrationBuilder.DropTable(
                name: "puzzles");

            migrationBuilder.DropTable(
                name: "telemetry_events");

            migrationBuilder.DropTable(
                name: "solve_sessions");
        }
    }
}
