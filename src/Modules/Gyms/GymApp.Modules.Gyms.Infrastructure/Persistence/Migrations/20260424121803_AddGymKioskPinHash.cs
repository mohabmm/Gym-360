using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gyms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGymKioskPinHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KioskPinHash",
                schema: "gyms",
                table: "gyms",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KioskPinHash",
                schema: "gyms",
                table: "gyms");
        }
    }
}
