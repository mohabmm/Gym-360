using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlanSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_plan_settings",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InactiveDays = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_plan_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_plan_settings_GymId",
                schema: "training",
                table: "training_plan_settings",
                column: "GymId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_plan_settings",
                schema: "training");
        }
    }
}
