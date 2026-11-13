using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Common.Infrastructure.FeatureFlags.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feature_flags");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "feature_flags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PresetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "features",
                schema: "feature_flags",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Apps = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_features", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "gym_overrides",
                schema: "feature_flags",
                columns: table => new
                {
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SetBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_overrides", x => new { x.GymId, x.FeatureKey });
                });

            migrationBuilder.CreateTable(
                name: "gym_presets",
                schema: "feature_flags",
                columns: table => new
                {
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_presets", x => x.GymId);
                });

            migrationBuilder.CreateTable(
                name: "preset_features",
                schema: "feature_flags",
                columns: table => new
                {
                    PresetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preset_features", x => new { x.PresetCode, x.FeatureKey });
                });

            migrationBuilder.CreateTable(
                name: "presets",
                schema: "feature_flags",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LimitsPlanCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presets", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_FeatureKey_OccurredAtUtc",
                schema: "feature_flags",
                table: "audit_log",
                columns: new[] { "FeatureKey", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_GymId_OccurredAtUtc",
                schema: "feature_flags",
                table: "audit_log",
                columns: new[] { "GymId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_OccurredAtUtc",
                schema: "feature_flags",
                table: "audit_log",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_gym_presets_PresetCode",
                schema: "feature_flags",
                table: "gym_presets",
                column: "PresetCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "feature_flags");

            migrationBuilder.DropTable(
                name: "features",
                schema: "feature_flags");

            migrationBuilder.DropTable(
                name: "gym_overrides",
                schema: "feature_flags");

            migrationBuilder.DropTable(
                name: "gym_presets",
                schema: "feature_flags");

            migrationBuilder.DropTable(
                name: "preset_features",
                schema: "feature_flags");

            migrationBuilder.DropTable(
                name: "presets",
                schema: "feature_flags");
        }
    }
}
