namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Schema-agnostic, idempotent T-SQL that adds the InBody composition columns (height, skeletal
/// muscle mass, body water) to <c>body_metric_entries</c> in every schema that has the table
/// (legacy <c>progress</c> and per-gym <c>g{N}_progress</c>). Per-gym tables are created from the
/// EF model and never migrated, so existing gyms need this on deploy. Mirrors the
/// <c>Composition</c> mapping in BodyMetricEntryConfiguration. Adds nullable columns only;
/// never alters or drops data.
/// </summary>
public static class BodyCompositionSchemaPatch
{
    public const string Sql = @"
SET QUOTED_IDENTIFIER ON;
DECLARE @patch nvarchar(max) = N'';
SELECT @patch = @patch + N'
IF COL_LENGTH(' + QUOTENAME(s.name + N'.body_metric_entries', '''') + N', ''composition_height_cm'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[body_metric_entries] ADD [composition_height_cm] decimal(5,1) NULL;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.body_metric_entries', '''') + N', ''composition_skeletal_muscle_kg'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[body_metric_entries] ADD [composition_skeletal_muscle_kg] decimal(6,2) NULL;
IF COL_LENGTH(' + QUOTENAME(s.name + N'.body_metric_entries', '''') + N', ''composition_body_water_kg'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.[body_metric_entries] ADD [composition_body_water_kg] decimal(6,2) NULL;'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.body_metric_entries', 'U') IS NOT NULL;
IF LEN(@patch) > 0 EXEC sp_executesql @patch;";
}
