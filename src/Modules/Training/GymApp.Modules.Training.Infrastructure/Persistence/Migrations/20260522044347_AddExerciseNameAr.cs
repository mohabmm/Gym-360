using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseNameAr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Arabic-name columns (CLAUDE.md rule 6 — Arabic first). These were
            // added to the Exercise/Program/Workout entities + their EF
            // configurations but never migrated, so every read threw
            // "Invalid column name 'NameAr'". All three are nullable; the
            // scaffolded UpdateData(value: null) seed no-ops are omitted.
            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "training",
                table: "workouts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "training",
                table: "programs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "training",
                table: "exercises",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "training",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "training",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "training",
                table: "exercises");
        }
    }
}
