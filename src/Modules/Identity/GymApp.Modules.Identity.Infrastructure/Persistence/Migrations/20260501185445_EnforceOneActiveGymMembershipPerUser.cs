using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneActiveGymMembershipPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-existing seed data may have multiple active memberships per user
            // (the legacy multi-gym-trainer model). Collapse to one active per user
            // before adding the filtered unique index. Keep the earliest joined_at.
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY joined_at ASC) AS rn
                    FROM [identity].[user_gym_memberships]
                    WHERE is_active = 1
                )
                UPDATE m SET is_active = 0
                FROM [identity].[user_gym_memberships] m
                INNER JOIN ranked r ON r.Id = m.Id
                WHERE r.rn > 1;");

            migrationBuilder.CreateIndex(
                name: "UX_user_gym_memberships_active_per_user",
                schema: "identity",
                table: "user_gym_memberships",
                column: "UserId",
                unique: true,
                filter: "[is_active] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_user_gym_memberships_active_per_user",
                schema: "identity",
                table: "user_gym_memberships");
        }
    }
}
