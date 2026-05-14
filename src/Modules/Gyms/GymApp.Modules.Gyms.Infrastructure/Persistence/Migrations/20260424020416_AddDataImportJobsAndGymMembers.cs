using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Gyms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataImportJobsAndGymMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: these tables are also created defensively by seed.sql Section 0
            // so a seed-then-boot sequence would otherwise crash at CreateTable with
            // "There is already an object named X".
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'gyms.data_import_jobs', N'U') IS NULL
    CREATE TABLE [gyms].[data_import_jobs] (
        [Id]               uniqueidentifier NOT NULL CONSTRAINT [PK_data_import_jobs] PRIMARY KEY,
        [GymId]            uniqueidentifier NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [Source]           int              NOT NULL,
        [Status]           int              NOT NULL,
        [OriginalFileName] nvarchar(260)    NOT NULL,
        [StoragePath]      nvarchar(500)    NOT NULL,
        [FileSizeBytes]    bigint           NOT NULL,
        [TotalRowCount]    int              NOT NULL,
        [SuccessCount]     int              NOT NULL,
        [FailureCount]     int              NOT NULL,
        [ErrorReportPath]  nvarchar(500)    NULL,
        [FailureReason]    nvarchar(1000)   NULL,
        [CreatedAt]        datetime2        NOT NULL,
        [StartedAt]        datetime2        NULL,
        [CompletedAt]      datetime2        NULL
    );

IF OBJECT_ID(N'gyms.gym_members', N'U') IS NULL
    CREATE TABLE [gyms].[gym_members] (
        [Id]           uniqueidentifier NOT NULL CONSTRAINT [PK_gym_members] PRIMARY KEY,
        [GymId]        uniqueidentifier NOT NULL,
        [MemberId]     uniqueidentifier NOT NULL,
        [MemberNumber] nvarchar(40)     NOT NULL,
        [Section]      nvarchar(60)     NULL,
        [JoinedAt]     datetime2        NOT NULL
    );

IF OBJECT_ID(N'gyms.member_notes', N'U') IS NULL
    CREATE TABLE [gyms].[member_notes] (
        [Id]        uniqueidentifier NOT NULL CONSTRAINT [PK_member_notes] PRIMARY KEY,
        [GymId]     uniqueidentifier NOT NULL,
        [MemberId]  uniqueidentifier NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [Content]   nvarchar(2000)   NOT NULL,
        [CreatedAt] datetime2        NOT NULL
    );

IF OBJECT_ID(N'gyms.member_onboardings', N'U') IS NULL
    CREATE TABLE [gyms].[member_onboardings] (
        [Id]             uniqueidentifier NOT NULL CONSTRAINT [PK_member_onboardings] PRIMARY KEY,
        [GymId]          uniqueidentifier NOT NULL,
        [MemberId]       uniqueidentifier NOT NULL,
        [MedicalHistory] nvarchar(2000)   NULL,
        [FitnessGoals]   nvarchar(2000)   NULL,
        [Injuries]       nvarchar(2000)   NULL,
        [Medications]    nvarchar(2000)   NULL,
        [FitnessLevel]   nvarchar(50)     NULL
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_data_import_jobs_GymId_CreatedAt' AND object_id = OBJECT_ID(N'gyms.data_import_jobs'))
    CREATE INDEX [IX_data_import_jobs_GymId_CreatedAt] ON [gyms].[data_import_jobs] ([GymId], [CreatedAt]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_data_import_jobs_Status' AND object_id = OBJECT_ID(N'gyms.data_import_jobs'))
    CREATE INDEX [IX_data_import_jobs_Status] ON [gyms].[data_import_jobs] ([Status]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gym_members_GymId_MemberId' AND object_id = OBJECT_ID(N'gyms.gym_members'))
    CREATE UNIQUE INDEX [IX_gym_members_GymId_MemberId] ON [gyms].[gym_members] ([GymId], [MemberId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gym_members_GymId_MemberNumber' AND object_id = OBJECT_ID(N'gyms.gym_members'))
    CREATE UNIQUE INDEX [IX_gym_members_GymId_MemberNumber] ON [gyms].[gym_members] ([GymId], [MemberNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_member_notes_GymId_MemberId' AND object_id = OBJECT_ID(N'gyms.member_notes'))
    CREATE INDEX [IX_member_notes_GymId_MemberId] ON [gyms].[member_notes] ([GymId], [MemberId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_member_onboardings_GymId_MemberId' AND object_id = OBJECT_ID(N'gyms.member_onboardings'))
    CREATE UNIQUE INDEX [IX_member_onboardings_GymId_MemberId] ON [gyms].[member_onboardings] ([GymId], [MemberId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "data_import_jobs", schema: "gyms");
            migrationBuilder.DropTable(name: "gym_members", schema: "gyms");
            migrationBuilder.DropTable(name: "member_notes", schema: "gyms");
            migrationBuilder.DropTable(name: "member_onboardings", schema: "gyms");
        }
    }
}
