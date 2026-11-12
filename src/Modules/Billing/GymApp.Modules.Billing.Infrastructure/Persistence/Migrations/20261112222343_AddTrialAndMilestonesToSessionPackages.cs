using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialAndMilestonesToSessionPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: in Hard tenancy the startup schema patch (PtSchemaPatch) also adds
            // these columns to every g{N}_billing.session_packages table.
            migrationBuilder.Sql(@"
IF COL_LENGTH('billing.session_packages', 'IsTrial') IS NULL
    ALTER TABLE [billing].[session_packages] ADD [IsTrial] bit NOT NULL CONSTRAINT [DF_billing_session_packages_IsTrial] DEFAULT 0;
IF COL_LENGTH('billing.session_packages', 'ExhaustedAt') IS NULL
    ALTER TABLE [billing].[session_packages] ADD [ExhaustedAt] datetime2 NULL;
IF COL_LENGTH('billing.session_packages', 'MilestonesSent') IS NULL
    ALTER TABLE [billing].[session_packages] ADD [MilestonesSent] int NOT NULL CONSTRAINT [DF_billing_session_packages_MilestonesSent] DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExhaustedAt",
                schema: "billing",
                table: "session_packages");

            migrationBuilder.DropColumn(
                name: "IsTrial",
                schema: "billing",
                table: "session_packages");

            migrationBuilder.DropColumn(
                name: "MilestonesSent",
                schema: "billing",
                table: "session_packages");
        }
    }
}
