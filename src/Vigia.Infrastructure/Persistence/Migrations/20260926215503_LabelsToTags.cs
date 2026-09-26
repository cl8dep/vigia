using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LabelsToTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "labels",
                table: "rules",
                newName: "tags");

            migrationBuilder.RenameColumn(
                name: "labels",
                table: "checks",
                newName: "tags");

            // System tag derived from each check's plugin.
            migrationBuilder.Sql("UPDATE checks SET tags = tags || jsonb_build_object('vigia:plugin', plugin);");

            // Selectors: { "plugin": "x", "k": "v" } -> { "vigia:plugin": ["x"], "k": ["v"] }.
            migrationBuilder.Sql("""
                UPDATE rules SET selector = coalesce(
                    (SELECT jsonb_object_agg(CASE WHEN key = 'plugin' THEN 'vigia:plugin' ELSE key END, jsonb_build_array(value))
                     FROM jsonb_each(selector)),
                    '{}'::jsonb);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE checks SET tags = tags - 'vigia:plugin';");
            migrationBuilder.Sql("""
                UPDATE rules SET selector = coalesce(
                    (SELECT jsonb_object_agg(CASE WHEN key = 'vigia:plugin' THEN 'plugin' ELSE key END, value -> 0)
                     FROM jsonb_each(selector)),
                    '{}'::jsonb);
                """);

            migrationBuilder.RenameColumn(
                name: "tags",
                table: "rules",
                newName: "labels");

            migrationBuilder.RenameColumn(
                name: "tags",
                table: "checks",
                newName: "labels");
        }
    }
}
