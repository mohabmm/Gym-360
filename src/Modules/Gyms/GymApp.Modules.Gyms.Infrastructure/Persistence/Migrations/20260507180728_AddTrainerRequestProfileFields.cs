using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gyms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerRequestProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                schema: "gyms",
                table: "gym_trainer_requests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specializations",
                schema: "gyms",
                table: "gym_trainer_requests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                schema: "gyms",
                table: "gym_trainer_requests");

            migrationBuilder.DropColumn(
                name: "Specializations",
                schema: "gyms",
                table: "gym_trainer_requests");
        }
    }
}
