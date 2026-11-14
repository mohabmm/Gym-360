using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: the startup schema patch (BodyCompositionSchemaPatch) runs first and may
            // already have added these columns to the legacy progress schema.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'progress.body_metric_entries', N'composition_height_cm') IS NULL
    ALTER TABLE [progress].[body_metric_entries] ADD [composition_height_cm] decimal(5,1) NULL;
IF COL_LENGTH(N'progress.body_metric_entries', N'composition_skeletal_muscle_kg') IS NULL
    ALTER TABLE [progress].[body_metric_entries] ADD [composition_skeletal_muscle_kg] decimal(6,2) NULL;
IF COL_LENGTH(N'progress.body_metric_entries', N'composition_body_water_kg') IS NULL
    ALTER TABLE [progress].[body_metric_entries] ADD [composition_body_water_kg] decimal(6,2) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "composition_body_water_kg",
                schema: "progress",
                table: "body_metric_entries");

            migrationBuilder.DropColumn(
                name: "composition_height_cm",
                schema: "progress",
                table: "body_metric_entries");

            migrationBuilder.DropColumn(
                name: "composition_skeletal_muscle_kg",
                schema: "progress",
                table: "body_metric_entries");
        }
    }
}
