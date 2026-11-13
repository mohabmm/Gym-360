namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Schema-agnostic, idempotent T-SQL that adds the PT-module columns to every
/// <c>session_packages</c> and <c>sessions</c> table (legacy and per-gym schemas).
/// Mirrors the EF model in SessionPackageConfiguration / SessionConfiguration.
/// </summary>
public static class PtSchemaPatch
{
    private const string AddColumn = @"
IF COL_LENGTH(' + QUOTENAME(s.name + N'.{0}', '''') + N', ''{1}'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.{0} ADD [{1}] {2};";

    private static readonly (string Table, string Column, string Definition)[] Columns =
    {
        ("session_packages", "IsTrial", "bit NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_session_packages_IsTrial') + N' DEFAULT 0"),
        ("session_packages", "ExhaustedAt", "datetime2 NULL"),
        ("session_packages", "MilestonesSent", "int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_session_packages_MilestonesSent') + N' DEFAULT 0"),
        ("sessions", "IsQuickLog", "bit NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_sessions_IsQuickLog') + N' DEFAULT 0"),
        ("sessions", "MemberConfirmation", "int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_sessions_MemberConfirmation') + N' DEFAULT 0"),
        ("sessions", "MemberRespondedAt", "datetime2 NULL"),
        ("sessions", "DisputeReason", "nvarchar(500) NULL"),
    };

    public static string Sql { get; } = Build();

    private static string Build()
    {
        var parts = Columns
            .GroupBy(c => c.Table)
            .Select(table => $@"
SELECT @patch = @patch + N'{string.Concat(table.Select(c => string.Format(AddColumn, c.Table, c.Column, c.Definition)))}'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.{table.Key}', 'U') IS NOT NULL;");

        return $@"
SET QUOTED_IDENTIFIER ON;
DECLARE @patch nvarchar(max) = N'';
{string.Concat(parts)}
IF LEN(@patch) > 0 EXEC sp_executesql @patch;";
    }
}
