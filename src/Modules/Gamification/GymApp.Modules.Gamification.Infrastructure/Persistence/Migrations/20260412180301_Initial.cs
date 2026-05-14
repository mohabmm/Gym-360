using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gamification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gamification");

            migrationBuilder.CreateTable(
                name: "leaderboard_opt_outs",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowOnLeaderboard = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_opt_outs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "member_leaderboards",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    period_month = table.Column<int>(type: "int", nullable: false),
                    period_year = table.Column<int>(type: "int", nullable: false),
                    LeaderboardType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_leaderboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainer_monthly_scores",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AggregatedScore = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalRatings = table.Column<int>(type: "int", nullable: false),
                    UniqueClients = table.Column<int>(type: "int", nullable: false),
                    SessionCount = table.Column<int>(type: "int", nullable: false),
                    NormalizedScore = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsCoachOfTheMonth = table.Column<bool>(type: "bit", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_monthly_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainer_public_profiles",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    CoachOfTheMonthCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_public_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainer_ratings",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stars = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_ratings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_entries",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaderboardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Delta = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    IsOptedIn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leaderboard_entries_member_leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalSchema: "gamification",
                        principalTable: "member_leaderboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testimonials",
                schema: "gamification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ModeratedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testimonials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testimonials_trainer_public_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "gamification",
                        principalTable: "trainer_public_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_LeaderboardId",
                schema: "gamification",
                table: "leaderboard_entries",
                column: "LeaderboardId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_entries_MemberId",
                schema: "gamification",
                table: "leaderboard_entries",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_opt_outs_UserId_GymId",
                schema: "gamification",
                table: "leaderboard_opt_outs",
                columns: new[] { "UserId", "GymId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_leaderboards_GymId",
                schema: "gamification",
                table: "member_leaderboards",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_testimonials_ProfileId",
                schema: "gamification",
                table: "testimonials",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_monthly_scores_GymId_TrainerId_Month_Year",
                schema: "gamification",
                table: "trainer_monthly_scores",
                columns: new[] { "GymId", "TrainerId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_public_profiles_TrainerId_GymId",
                schema: "gamification",
                table: "trainer_public_profiles",
                columns: new[] { "TrainerId", "GymId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_ratings_SessionId",
                schema: "gamification",
                table: "trainer_ratings",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_ratings_TrainerId_GymId",
                schema: "gamification",
                table: "trainer_ratings",
                columns: new[] { "TrainerId", "GymId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leaderboard_entries",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "leaderboard_opt_outs",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "testimonials",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "trainer_monthly_scores",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "trainer_ratings",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "member_leaderboards",
                schema: "gamification");

            migrationBuilder.DropTable(
                name: "trainer_public_profiles",
                schema: "gamification");
        }
    }
}
