using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAgentToWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "agent",
                table: "check_results",
                newName: "worker");

            migrationBuilder.RenameColumn(
                name: "agent",
                table: "check_result_rollups",
                newName: "worker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "worker",
                table: "check_results",
                newName: "agent");

            migrationBuilder.RenameColumn(
                name: "worker",
                table: "check_result_rollups",
                newName: "agent");
        }
    }
}
