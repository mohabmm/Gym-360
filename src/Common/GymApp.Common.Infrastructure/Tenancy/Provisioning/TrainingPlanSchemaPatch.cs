namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Schema-agnostic, idempotent T-SQL that creates the pt.training_plan tables
/// (<c>plan_templates</c>, <c>training_plans</c>, <c>plan_workout_completions</c>,
/// <c>training_plan_settings</c>, <c>challenge_awards</c>) and the first-month challenge columns
/// in every schema that already has <c>workout_assignments</c> (legacy <c>training</c> and per-gym
/// <c>g{N}_training</c>). Per-gym tables are created from the EF model and never migrated, so
/// existing gyms need this on deploy — without it saving a plan or template fails with
/// "Invalid object name". Mirrors the Training configurations and the 2026-11-23 migrations.
/// Creates and adds only; never alters or drops data.
/// </summary>
public static class TrainingPlanSchemaPatch
{
    public const string Sql = @"
SET QUOTED_IDENTIFIER ON;
DECLARE @planPatch nvarchar(max) = N'';
SELECT @planPatch = @planPatch + N'
IF OBJECT_ID(' + QUOTENAME(s.name + N'.plan_templates', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[plan_templates] (
        [Id]             uniqueidentifier NOT NULL,
        [GymId]          uniqueidentifier NOT NULL,
        [Scope]          nvarchar(20)     NOT NULL,
        [OwnerTrainerId] uniqueidentifier NULL,
        [CreatedBy]      uniqueidentifier NOT NULL,
        [Name]           nvarchar(200)    NOT NULL,
        [NameAr]         nvarchar(200)    NULL,
        [Goal]           nvarchar(30)     NOT NULL,
        [GoalText]       nvarchar(200)    NULL,
        [DurationWeeks]  int              NOT NULL,
        [DaysPerWeek]    int              NOT NULL,
        [document]       nvarchar(max)    NOT NULL,
        [SampleKey]      nvarchar(50)     NULL,
        [IsArchived]     bit              NOT NULL,
        [CreatedAt]      datetime2        NOT NULL,
        [UpdatedAt]      datetime2        NOT NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_plan_templates') + N' PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_plan_templates_GymId_OwnerTrainerId] ON ' + QUOTENAME(s.name) + N'.[plan_templates] ([GymId], [OwnerTrainerId]);
    CREATE INDEX [IX_plan_templates_GymId_Scope_IsArchived] ON ' + QUOTENAME(s.name) + N'.[plan_templates] ([GymId], [Scope], [IsArchived]);
    CREATE UNIQUE INDEX [UX_plan_templates_sample] ON ' + QUOTENAME(s.name) + N'.[plan_templates] ([GymId], [SampleKey]) WHERE [SampleKey] IS NOT NULL;
END;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.training_plans', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[training_plans] (
        [Id]               uniqueidentifier NOT NULL,
        [GymId]            uniqueidentifier NOT NULL,
        [TraineeId]        uniqueidentifier NOT NULL,
        [TrainerId]        uniqueidentifier NOT NULL,
        [SourceTemplateId] uniqueidentifier NULL,
        [Name]             nvarchar(200)    NOT NULL,
        [NameAr]           nvarchar(200)    NULL,
        [Goal]             nvarchar(30)     NOT NULL,
        [GoalText]         nvarchar(200)    NULL,
        [DurationWeeks]    int              NOT NULL,
        [DaysPerWeek]      int              NOT NULL,
        [document]         nvarchar(max)    NOT NULL,
        [StartDate]        date             NOT NULL,
        [Status]           nvarchar(20)     NOT NULL,
        [Version]          int              NOT NULL,
        [CreatedAt]        datetime2        NOT NULL,
        [UpdatedAt]        datetime2        NOT NULL,
        [EndedAt]          datetime2        NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_training_plans') + N' PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_training_plans_GymId_TraineeId_CreatedAt] ON ' + QUOTENAME(s.name) + N'.[training_plans] ([GymId], [TraineeId], [CreatedAt]);
    CREATE INDEX [IX_training_plans_GymId_TrainerId_Status] ON ' + QUOTENAME(s.name) + N'.[training_plans] ([GymId], [TrainerId], [Status]);
    CREATE UNIQUE INDEX [UX_training_plans_active_trainee] ON ' + QUOTENAME(s.name) + N'.[training_plans] ([GymId], [TraineeId]) WHERE [Status] = ''Active'';
END;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.plan_workout_completions', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[plan_workout_completions] (
        [Id]              uniqueidentifier NOT NULL,
        [GymId]           uniqueidentifier NOT NULL,
        [PlanId]          uniqueidentifier NOT NULL,
        [TraineeId]       uniqueidentifier NOT NULL,
        [ClientId]        uniqueidentifier NOT NULL,
        [DayKey]          nvarchar(50)     NOT NULL,
        [DayTitle]        nvarchar(200)    NOT NULL,
        [DayTitleAr]      nvarchar(200)    NULL,
        [Week]            int              NOT NULL,
        [SequenceIndex]   int              NOT NULL,
        [PlanVersion]     int              NOT NULL,
        [CompletedAt]     datetime2        NOT NULL,
        [CompletedOn]     date             NOT NULL,
        [ReceivedAt]      datetime2        NOT NULL,
        [Source]          nvarchar(20)     NOT NULL,
        [SessionId]       uniqueidentifier NULL,
        [DurationMinutes] int              NULL,
        [Note]            nvarchar(500)    NULL,
        [sets]            nvarchar(max)    NOT NULL,
        [IsVoided]        bit              NOT NULL,
        [VoidedAt]        datetime2        NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_plan_workout_completions') + N' PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_plan_workout_completions_GymId_PlanId] ON ' + QUOTENAME(s.name) + N'.[plan_workout_completions] ([GymId], [PlanId]);
    CREATE INDEX [IX_plan_workout_completions_GymId_SessionId] ON ' + QUOTENAME(s.name) + N'.[plan_workout_completions] ([GymId], [SessionId]) WHERE [SessionId] IS NOT NULL;
    CREATE INDEX [IX_plan_workout_completions_GymId_TraineeId_CompletedOn] ON ' + QUOTENAME(s.name) + N'.[plan_workout_completions] ([GymId], [TraineeId], [CompletedOn]);
    CREATE UNIQUE INDEX [UX_plan_workout_completions_client] ON ' + QUOTENAME(s.name) + N'.[plan_workout_completions] ([TraineeId], [ClientId]);
END;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.training_plan_settings', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[training_plan_settings] (
        [Id]                  uniqueidentifier NOT NULL,
        [GymId]               uniqueidentifier NOT NULL,
        [InactiveDays]        int              NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_tps_InactiveDays') + N' DEFAULT 5,
        [UpdatedAt]           datetime2        NOT NULL,
        [UpdatedBy]           uniqueidentifier NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_training_plan_settings') + N' PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_training_plan_settings_GymId] ON ' + QUOTENAME(s.name) + N'.[training_plan_settings] ([GymId]);
END;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.training_plan_settings', '''') + N', ''ChallengeTarget'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[training_plan_settings]
        ADD [ChallengeTarget] int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_tps_ChallengeTarget') + N' DEFAULT 12;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.training_plan_settings', '''') + N', ''ChallengeWindowDays'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[training_plan_settings]
        ADD [ChallengeWindowDays] int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_tps_ChallengeWindowDays') + N' DEFAULT 30;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.training_plan_settings', '''') + N', ''RewardTextAr'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[training_plan_settings] ADD [RewardTextAr] nvarchar(300) NULL;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.training_plan_settings', '''') + N', ''RewardTextEn'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[training_plan_settings] ADD [RewardTextEn] nvarchar(300) NULL;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.challenge_awards', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[challenge_awards] (
        [Id]           uniqueidentifier NOT NULL,
        [GymId]        uniqueidentifier NOT NULL,
        [TraineeId]    uniqueidentifier NOT NULL,
        [TrainerId]    uniqueidentifier NULL,
        [PlanId]       uniqueidentifier NULL,
        [WindowStart]  date             NOT NULL,
        [WindowEnd]    date             NOT NULL,
        [Target]       int              NOT NULL,
        [CountAtReach] int              NOT NULL,
        [ReachedAt]    datetime2        NOT NULL,
        [RewardTextAr] nvarchar(300)    NOT NULL,
        [RewardTextEn] nvarchar(300)    NOT NULL,
        [DeliveredAt]  datetime2        NULL,
        [DeliveredBy]  uniqueidentifier NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_challenge_awards') + N' PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_challenge_awards_GymId_DeliveredAt] ON ' + QUOTENAME(s.name) + N'.[challenge_awards] ([GymId], [DeliveredAt]);
    CREATE UNIQUE INDEX [IX_challenge_awards_GymId_TraineeId] ON ' + QUOTENAME(s.name) + N'.[challenge_awards] ([GymId], [TraineeId]);
END;'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.workout_assignments', 'U') IS NOT NULL;
IF LEN(@planPatch) > 0 EXEC sp_executesql @planPatch;";
}
