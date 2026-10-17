using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryCountToOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: an earlier DatabaseInitializer outbox backfill (before it excluded
            // the migration-owned 'identity' schema) may already have added this column.
            migrationBuilder.Sql(@"
IF COL_LENGTH('identity.outbox_messages', 'RetryCount') IS NULL
    ALTER TABLE [identity].[outbox_messages] ADD [RetryCount] int NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "identity",
                table: "outbox_messages");
        }
    }
}
