using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanWorkoutCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_workout_completions",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DayTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DayTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Week = table.Column<int>(type: "int", nullable: false),
                    SequenceIndex = table.Column<int>(type: "int", nullable: false),
                    PlanVersion = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_workout_completions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_workout_completions_GymId_PlanId",
                schema: "training",
                table: "plan_workout_completions",
                columns: new[] { "GymId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_workout_completions_GymId_SessionId",
                schema: "training",
                table: "plan_workout_completions",
                columns: new[] { "GymId", "SessionId" },
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_plan_workout_completions_GymId_TraineeId_CompletedOn",
                schema: "training",
                table: "plan_workout_completions",
                columns: new[] { "GymId", "TraineeId", "CompletedOn" });

            migrationBuilder.CreateIndex(
                name: "UX_plan_workout_completions_client",
                schema: "training",
                table: "plan_workout_completions",
                columns: new[] { "TraineeId", "ClientId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_workout_completions",
                schema: "training");
        }
    }
}
