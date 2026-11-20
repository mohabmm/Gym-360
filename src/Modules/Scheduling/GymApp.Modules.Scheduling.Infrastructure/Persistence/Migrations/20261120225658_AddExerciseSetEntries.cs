using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSetEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: in Hard tenancy the startup schema patch (PtSchemaPatch) also adds
            // these columns to every g{N}_scheduling.session_exercise_logs table.
            migrationBuilder.Sql(@"
IF COL_LENGTH('scheduling.session_exercise_logs', 'Measure') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [Measure] int NOT NULL CONSTRAINT [DF_scheduling_session_exercise_logs_Measure] DEFAULT 0;
IF COL_LENGTH('scheduling.session_exercise_logs', 'SetEntries') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [SetEntries] nvarchar(max) NULL;
IF COL_LENGTH('scheduling.session_exercise_logs', 'TargetSeconds') IS NULL
    ALTER TABLE [scheduling].[session_exercise_logs] ADD [TargetSeconds] int NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Measure",
                schema: "scheduling",
                table: "session_exercise_logs");

            migrationBuilder.DropColumn(
                name: "SetEntries",
                schema: "scheduling",
                table: "session_exercise_logs");

            migrationBuilder.DropColumn(
                name: "TargetSeconds",
                schema: "scheduling",
                table: "session_exercise_logs");
        }
    }
}
