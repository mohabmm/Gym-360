namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Schema-agnostic, idempotent T-SQL that creates the <c>package_interests</c> table
/// (pt.package_interest) in every schema that already has <c>session_packages</c>
/// (legacy <c>billing</c> and per-gym <c>g{N}_billing</c>). Per-gym tables are created from
/// the EF model and never migrated, so existing gyms need this on deploy. Mirrors
/// PackageInterestConfiguration. Creates only; never alters or drops data.
/// </summary>
public static class PackageInterestsSchemaPatch
{
    public const string Sql = @"
SET QUOTED_IDENTIFIER ON;
DECLARE @interestsPatch nvarchar(max) = N'';
SELECT @interestsPatch = @interestsPatch + N'
IF OBJECT_ID(' + QUOTENAME(s.name + N'.package_interests', '''') + N', ''U'') IS NULL
BEGIN
    CREATE TABLE ' + QUOTENAME(s.name) + N'.[package_interests] (
        [Id]                  uniqueidentifier NOT NULL,
        [GymId]               uniqueidentifier NOT NULL,
        [MemberId]            uniqueidentifier NOT NULL,
        [TrainerId]           uniqueidentifier NOT NULL,
        [SourcePackageId]     uniqueidentifier NOT NULL,
        [Source]              nvarchar(20)     NOT NULL,
        [RecommendedSessions] int              NULL,
        [Status]              nvarchar(20)     NOT NULL,
        [CreatedAt]           datetime2        NOT NULL,
        [DismissedAt]         datetime2        NULL,
        [DismissedBy]         uniqueidentifier NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_package_interests') + N' PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_package_interests_GymId_MemberId] ON ' + QUOTENAME(s.name) + N'.[package_interests] ([GymId], [MemberId]);
END;'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.session_packages', 'U') IS NOT NULL;
IF LEN(@interestsPatch) > 0 EXEC sp_executesql @interestsPatch;";
}
