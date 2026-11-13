namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Schema-agnostic, idempotent T-SQL that creates the <c>progress_goals</c> table
/// (pt.progress_measurements) in every schema that already has <c>body_metric_entries</c>
/// (legacy <c>progress</c> and per-gym <c>g{N}_progress</c>). Per-gym tables are created from
/// the EF model and never migrated, so existing gyms need this on deploy. Mirrors
/// ProgressGoalConfiguration. Creates only; never alters or drops data.
/// </summary>
public static class ProgressGoalsSchemaPatch
{
    public const string Sql = @"
SET QUOTED_IDENTIFIER ON;
DECLARE @patch nvarchar(max) = N'';
SELECT @patch = @patch + N'
IF OBJECT_ID(' + QUOTENAME(s.name + N'.progress_goals', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[progress_goals] (
        [Id]           uniqueidentifier NOT NULL,
        [UserId]       uniqueidentifier NOT NULL,
        [GymId]        uniqueidentifier NOT NULL,
        [Goal]         nvarchar(20)     NOT NULL,
        [TargetWeight] decimal(8,2)     NULL,
        [SetBy]        uniqueidentifier NOT NULL,
        [UpdatedAt]    datetime2        NOT NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_progress_goals') + N' PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_progress_goals_user_gym] ON ' + QUOTENAME(s.name) + N'.[progress_goals] ([UserId], [GymId]);
END;'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.body_metric_entries', 'U') IS NOT NULL;
IF LEN(@patch) > 0 EXEC sp_executesql @patch;";
}
