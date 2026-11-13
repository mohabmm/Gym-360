using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickLogAndMemberConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: in Hard tenancy the startup schema patch (PtSchemaPatch) also adds
            // these columns to every g{N}_scheduling.sessions table.
            migrationBuilder.Sql(@"
IF COL_LENGTH('scheduling.sessions', 'IsQuickLog') IS NULL
    ALTER TABLE [scheduling].[sessions] ADD [IsQuickLog] bit NOT NULL CONSTRAINT [DF_scheduling_sessions_IsQuickLog] DEFAULT 0;
IF COL_LENGTH('scheduling.sessions', 'MemberConfirmation') IS NULL
    ALTER TABLE [scheduling].[sessions] ADD [MemberConfirmation] int NOT NULL CONSTRAINT [DF_scheduling_sessions_MemberConfirmation] DEFAULT 0;
IF COL_LENGTH('scheduling.sessions', 'MemberRespondedAt') IS NULL
    ALTER TABLE [scheduling].[sessions] ADD [MemberRespondedAt] datetime2 NULL;
IF COL_LENGTH('scheduling.sessions', 'DisputeReason') IS NULL
    ALTER TABLE [scheduling].[sessions] ADD [DisputeReason] nvarchar(500) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeReason",
                schema: "scheduling",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "IsQuickLog",
                schema: "scheduling",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "MemberConfirmation",
                schema: "scheduling",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "MemberRespondedAt",
                schema: "scheduling",
                table: "sessions");
        }
    }
}
