using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.EnsureSchema(
                name: "gyms");

            migrationBuilder.EnsureSchema(
                name: "billing");

            // gyms.gym_session_policies is owned by the Gyms module — this module only reads it.

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            // billing.session_packages is owned by the Billing module — this module only reads it.

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Focus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecurrenceRule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainer_availabilities",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_availabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_member_logs",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    PerceivedExertion = table.Column<int>(type: "int", nullable: false),
                    PersonalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Discomfort = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Satisfaction = table.Column<int>(type: "int", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_member_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_member_logs_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "scheduling",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_trainer_logs",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExercisesPerformed = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ActualVsPlanned = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClientEffortRating = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TechniqueNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AreasToImprove = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NextSessionFocus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttendanceConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_trainer_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_trainer_logs_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "scheduling",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_slots",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerAvailabilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_availability_slots_trainer_availabilities_TrainerAvailabilityId",
                        column: x => x.TrainerAvailabilityId,
                        principalSchema: "scheduling",
                        principalTable: "trainer_availabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blocked_dates",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerAvailabilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AutoNotifyClients = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_dates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_blocked_dates_trainer_availabilities_TrainerAvailabilityId",
                        column: x => x.TrainerAvailabilityId,
                        principalSchema: "scheduling",
                        principalTable: "trainer_availabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_availability_slots_availability_day",
                schema: "scheduling",
                table: "availability_slots",
                columns: new[] { "TrainerAvailabilityId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_blocked_dates_availability_date",
                schema: "scheduling",
                table: "blocked_dates",
                columns: new[] { "TrainerAvailabilityId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "scheduling",
                table: "outbox_messages",
                column: "ProcessedOnUtc",
                filter: "[ProcessedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_session_member_logs_session",
                schema: "scheduling",
                table: "session_member_logs",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_trainer_logs_session",
                schema: "scheduling",
                table: "session_trainer_logs",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_gym_status",
                schema: "scheduling",
                table: "sessions",
                columns: new[] { "GymId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_member_date",
                schema: "scheduling",
                table: "sessions",
                columns: new[] { "MemberId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_trainer_date",
                schema: "scheduling",
                table: "sessions",
                columns: new[] { "TrainerId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_availabilities_trainer_gym",
                schema: "scheduling",
                table: "trainer_availabilities",
                columns: new[] { "TrainerId", "GymId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_slots",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "blocked_dates",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "session_member_logs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "session_trainer_logs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "trainer_availabilities",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "scheduling");
        }
    }
}
