using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: in Hard tenancy the startup schema patch (PtSchemaPatch) also adds
            // these columns to every g{N}_scheduling table.
            migrationBuilder.Sql(@"
IF COL_LENGTH('scheduling.sessions', 'DayLabel') IS NULL
    ALTER TABLE [scheduling].[sessions] ADD [DayLabel] nvarchar(40) NULL;
IF COL_LENGTH('scheduling.session_exercise_logs', 'TargetSets') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [TargetSets] int NULL;
IF COL_LENGTH('scheduling.session_exercise_logs', 'TargetReps') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [TargetReps] nvarchar(50) NULL;
IF COL_LENGTH('scheduling.session_exercise_logs', 'TargetWeight') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [TargetWeight] decimal(8,2) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayLabel",
                schema: "scheduling",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "TargetReps",
                schema: "scheduling",
                table: "session_exercise_logs");

            migrationBuilder.DropColumn(
                name: "TargetSets",
                schema: "scheduling",
                table: "session_exercise_logs");

            migrationBuilder.DropColumn(
                name: "TargetWeight",
                schema: "scheduling",
                table: "session_exercise_logs");
        }
    }
}
