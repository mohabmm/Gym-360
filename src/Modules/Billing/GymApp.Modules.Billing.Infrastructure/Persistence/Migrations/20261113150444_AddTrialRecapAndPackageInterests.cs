using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialRecapAndPackageInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecapNote",
                schema: "billing",
                table: "session_packages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecapSentAt",
                schema: "billing",
                table: "session_packages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecommendedSessions",
                schema: "billing",
                table: "session_packages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "package_interests",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecommendedSessions = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_interests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_package_interests_GymId_MemberId",
                schema: "billing",
                table: "package_interests",
                columns: new[] { "GymId", "MemberId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "package_interests",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "RecapNote",
                schema: "billing",
                table: "session_packages");

            migrationBuilder.DropColumn(
                name: "RecapSentAt",
                schema: "billing",
                table: "session_packages");

            migrationBuilder.DropColumn(
                name: "RecommendedSessions",
                schema: "billing",
                table: "session_packages");
        }
    }
}
