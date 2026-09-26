using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    outcome = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    measurements = table.Column<string>(type: "jsonb", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    duration_ms = table.Column<double>(type: "double precision", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_check_results_checks_check_id",
                        column: x => x.check_id,
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_check_results_check_id_observed_at",
                table: "check_results",
                columns: new[] { "check_id", "observed_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_results");
        }
    }
}
