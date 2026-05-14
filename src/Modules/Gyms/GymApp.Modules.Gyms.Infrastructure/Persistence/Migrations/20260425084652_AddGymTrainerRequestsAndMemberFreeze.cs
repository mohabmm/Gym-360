using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gyms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGymTrainerRequestsAndMemberFreeze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FrozenAt",
                schema: "gyms",
                table: "gym_members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                schema: "gyms",
                table: "gym_members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "gym_trainer_requests",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_trainer_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gym_trainer_requests_GymId_Status",
                schema: "gyms",
                table: "gym_trainer_requests",
                columns: new[] { "GymId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_gym_trainer_requests_GymId_TrainerId",
                schema: "gyms",
                table: "gym_trainer_requests",
                columns: new[] { "GymId", "TrainerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gym_trainer_requests",
                schema: "gyms");

            migrationBuilder.DropColumn(
                name: "FrozenAt",
                schema: "gyms",
                table: "gym_members");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                schema: "gyms",
                table: "gym_members");
        }
    }
}
