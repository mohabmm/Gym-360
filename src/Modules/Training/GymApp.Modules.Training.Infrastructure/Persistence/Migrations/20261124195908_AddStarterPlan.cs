using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStarterPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndsOn",
                schema: "training",
                table: "training_plans",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Limit",
                schema: "training",
                table: "training_plans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateTable(
                name: "plan_program_unlocks",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Length = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "OneWeek"),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnlockedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_program_unlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_program_unlocks_GymId_TraineeId_TrainerId",
                schema: "training",
                table: "plan_program_unlocks",
                columns: new[] { "GymId", "TraineeId", "TrainerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_program_unlocks",
                schema: "training");

            migrationBuilder.DropColumn(
                name: "EndsOn",
                schema: "training",
                table: "training_plans");

            migrationBuilder.DropColumn(
                name: "Limit",
                schema: "training",
                table: "training_plans");
        }
    }
}
