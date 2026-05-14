using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gyms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gyms");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "announcements",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "check_ins",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_ins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Schedule_DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Schedule_StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Schedule_DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Schedule_IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    WaitlistEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gym_seasonal_hours",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_seasonal_hours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gym_sections",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_sections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gym_session_policies",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CancellationWindowHours = table.Column<int>(type: "int", nullable: false),
                    NoshowThreshold = table.Column<int>(type: "int", nullable: false),
                    NoshowWindowDays = table.Column<int>(type: "int", nullable: false),
                    NoshowAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_session_policies", x => x.Id);
                });

            // billing.gym_subscriptions is owned by the Billing module — this module only reads it.

            migrationBuilder.CreateTable(
                name: "gyms",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address_Latitude = table.Column<double>(type: "float", nullable: true),
                    Address_Longitude = table.Column<double>(type: "float", nullable: true),
                    Contact_Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Contact_Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contact_Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Branding_LogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Branding_PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Branding_SecondaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Branding_CoverPhotoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GymCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Social_Facebook = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Social_Instagram = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Social_Twitter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Social_Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GracePeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amenities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MemberIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrainerIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_role_permissions",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_role_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_roles",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_bookings",
                schema: "gyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_bookings_classes_GymClassId",
                        column: x => x.GymClassId,
                        principalSchema: "gyms",
                        principalTable: "classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gym_seasonal_day_schedules",
                schema: "gyms",
                columns: table => new
                {
                    GymSeasonalHoursId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_seasonal_day_schedules", x => new { x.GymSeasonalHoursId, x.Id });
                    table.ForeignKey(
                        name: "FK_gym_seasonal_day_schedules_gym_seasonal_hours_GymSeasonalHoursId",
                        column: x => x.GymSeasonalHoursId,
                        principalSchema: "gyms",
                        principalTable: "gym_seasonal_hours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gym_operating_hours",
                schema: "gyms",
                columns: table => new
                {
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gym_operating_hours", x => new { x.GymId, x.Id });
                    table.ForeignKey(
                        name: "FK_gym_operating_hours_gyms_GymId",
                        column: x => x.GymId,
                        principalSchema: "gyms",
                        principalTable: "gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_GymId",
                schema: "gyms",
                table: "announcements",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_check_ins_GymId_MemberId_CheckedInAt",
                schema: "gyms",
                table: "check_ins",
                columns: new[] { "GymId", "MemberId", "CheckedInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_class_bookings_GymClassId",
                schema: "gyms",
                table: "class_bookings",
                column: "GymClassId");

            migrationBuilder.CreateIndex(
                name: "IX_class_bookings_MemberId",
                schema: "gyms",
                table: "class_bookings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_classes_GymId",
                schema: "gyms",
                table: "classes",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_classes_TrainerId",
                schema: "gyms",
                table: "classes",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_gym_seasonal_hours_GymId",
                schema: "gyms",
                table: "gym_seasonal_hours",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_gym_sections_GymId",
                schema: "gyms",
                table: "gym_sections",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_gym_session_policies_GymId",
                schema: "gyms",
                table: "gym_session_policies",
                column: "GymId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gyms_GymCode",
                schema: "gyms",
                table: "gyms",
                column: "GymCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gyms_OwnerId",
                schema: "gyms",
                table: "gyms",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_permissions_GymId_StaffRole_PermissionCode",
                schema: "gyms",
                table: "staff_role_permissions",
                columns: new[] { "GymId", "StaffRole", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_roles_GymId_UserId",
                schema: "gyms",
                table: "staff_roles",
                columns: new[] { "GymId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "check_ins",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "class_bookings",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gym_operating_hours",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gym_seasonal_day_schedules",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gym_sections",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gym_session_policies",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "staff_role_permissions",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "staff_roles",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "classes",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gyms",
                schema: "gyms");

            migrationBuilder.DropTable(
                name: "gym_seasonal_hours",
                schema: "gyms");
        }
    }
}
