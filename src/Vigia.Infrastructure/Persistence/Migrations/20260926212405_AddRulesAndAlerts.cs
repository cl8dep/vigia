using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesAndAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selector = table.Column<string>(type: "jsonb", nullable: false),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    dimension = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    threshold = table.Column<double>(type: "double precision", nullable: true),
                    @for = table.Column<int>(name: "for", type: "integer", nullable: false),
                    recover_after = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    labels = table.Column<string>(type: "jsonb", nullable: false),
                    managed_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_rules_checks_check_id",
                        column: x => x.check_id,
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    fired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    occurrences = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerts_checks_check_id",
                        column: x => x.check_id,
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alerts_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_check_id",
                table: "alerts",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_state_fired_at",
                table: "alerts",
                columns: new[] { "state", "fired_at" });

            migrationBuilder.CreateIndex(
                name: "ux_alerts_firing_rule_check",
                table: "alerts",
                columns: new[] { "rule_id", "check_id" },
                unique: true,
                filter: "state = 'Firing'");

            migrationBuilder.CreateIndex(
                name: "ix_rules_check_id",
                table: "rules",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_rules_slug",
                table: "rules",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "rules");
        }
    }
}
