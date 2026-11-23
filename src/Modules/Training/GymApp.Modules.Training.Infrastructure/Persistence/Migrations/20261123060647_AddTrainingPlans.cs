using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_templates",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OwnerTrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Goal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GoalText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DurationWeeks = table.Column<int>(type: "int", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "int", nullable: false),
                    document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "training_plans",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Goal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GoalText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DurationWeeks = table.Column<int>(type: "int", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "int", nullable: false),
                    document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_plans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_templates_GymId_OwnerTrainerId",
                schema: "training",
                table: "plan_templates",
                columns: new[] { "GymId", "OwnerTrainerId" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_templates_GymId_Scope_IsArchived",
                schema: "training",
                table: "plan_templates",
                columns: new[] { "GymId", "Scope", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "UX_plan_templates_sample",
                schema: "training",
                table: "plan_templates",
                columns: new[] { "GymId", "SampleKey" },
                unique: true,
                filter: "[SampleKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_training_plans_GymId_TraineeId_CreatedAt",
                schema: "training",
                table: "training_plans",
                columns: new[] { "GymId", "TraineeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_training_plans_GymId_TrainerId_Status",
                schema: "training",
                table: "training_plans",
                columns: new[] { "GymId", "TrainerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_training_plans_active_trainee",
                schema: "training",
                table: "training_plans",
                columns: new[] { "GymId", "TraineeId" },
                unique: true,
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_templates",
                schema: "training");

            migrationBuilder.DropTable(
                name: "training_plans",
                schema: "training");
        }
    }
}
