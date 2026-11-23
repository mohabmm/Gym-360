using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstMonthChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChallengeTarget",
                schema: "training",
                table: "training_plan_settings",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "ChallengeWindowDays",
                schema: "training",
                table: "training_plan_settings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "RewardTextAr",
                schema: "training",
                table: "training_plan_settings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RewardTextEn",
                schema: "training",
                table: "training_plan_settings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "challenge_awards",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WindowStart = table.Column<DateOnly>(type: "date", nullable: false),
                    WindowEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    CountAtReach = table.Column<int>(type: "int", nullable: false),
                    ReachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RewardTextAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RewardTextEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_awards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_awards_GymId_DeliveredAt",
                schema: "training",
                table: "challenge_awards",
                columns: new[] { "GymId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_awards_GymId_TraineeId",
                schema: "training",
                table: "challenge_awards",
                columns: new[] { "GymId", "TraineeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "challenge_awards",
                schema: "training");

            migrationBuilder.DropColumn(
                name: "ChallengeTarget",
                schema: "training",
                table: "training_plan_settings");

            migrationBuilder.DropColumn(
                name: "ChallengeWindowDays",
                schema: "training",
                table: "training_plan_settings");

            migrationBuilder.DropColumn(
                name: "RewardTextAr",
                schema: "training",
                table: "training_plan_settings");

            migrationBuilder.DropColumn(
                name: "RewardTextEn",
                schema: "training",
                table: "training_plan_settings");
        }
    }
}
