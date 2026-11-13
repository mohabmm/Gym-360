using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: the startup schema patch (ProgressGoalsSchemaPatch) runs first and may
            // already have created this table in the legacy progress schema.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'progress.progress_goals', 'U') IS NULL
BEGIN
    CREATE TABLE [progress].[progress_goals] (
        [Id]           uniqueidentifier NOT NULL,
        [UserId]       uniqueidentifier NOT NULL,
        [GymId]        uniqueidentifier NOT NULL,
        [Goal]         nvarchar(20)     NOT NULL,
        [TargetWeight] decimal(8,2)     NULL,
        [SetBy]        uniqueidentifier NOT NULL,
        [UpdatedAt]    datetime2        NOT NULL,
        CONSTRAINT [PK_progress_goals] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_progress_goals_user_gym] ON [progress].[progress_goals] ([UserId], [GymId]);
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "progress_goals",
                schema: "progress");
        }
    }
}
