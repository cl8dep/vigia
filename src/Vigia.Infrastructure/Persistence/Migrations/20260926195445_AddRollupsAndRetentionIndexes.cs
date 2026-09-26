using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRollupsAndRetentionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_result_rollups",
                columns: table => new
                {
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hour_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    up = table.Column<long>(type: "bigint", nullable: false),
                    down = table.Column<long>(type: "bigint", nullable: false),
                    error = table.Column<long>(type: "bigint", nullable: false),
                    dimensions = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_result_rollups", x => new { x.check_id, x.agent, x.hour_start });
                    table.ForeignKey(
                        name: "fk_check_result_rollups_checks_check_id",
                        column: x => x.check_id,
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_check_results_observed_at",
                table: "check_results",
                column: "observed_at");

            migrationBuilder.CreateIndex(
                name: "ix_check_result_rollups_hour_start",
                table: "check_result_rollups",
                column: "hour_start");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_result_rollups");

            migrationBuilder.DropIndex(
                name: "ix_check_results_observed_at",
                table: "check_results");
        }
    }
}
