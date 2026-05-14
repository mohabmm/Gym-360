using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// Creates the per-gym SQL Server schemas required before EF migrations run.
/// EF's <c>HasDefaultSchema</c> does not auto-create the schema, so we issue
/// raw <c>CREATE SCHEMA</c> statements first.
///
/// SchemaPrefix is validated against <c>^g\d+$</c> to prevent dynamic-SQL injection.
/// </summary>
public sealed class SchemaCreator
{
    public static readonly IReadOnlyList<string> PerGymModuleSchemas = new[]
    {
        "gyms", "training", "progress", "scheduling",
        "nutrition", "communication", "billing", "gamification", "outbox"
    };

    private static readonly Regex PrefixPattern = new(@"^g\d+$", RegexOptions.Compiled);

    public async Task CreateSchemasAsync(
        DbContext anyDbContext,
        string schemaPrefix,
        CancellationToken cancellationToken = default)
    {
        ValidatePrefix(schemaPrefix);

        var connection = anyDbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        foreach (var module in PerGymModuleSchemas)
        {
            var schemaName = $"{schemaPrefix}_{module}";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schemaName}') EXEC('CREATE SCHEMA [{schemaName}]');";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DropSchemasAsync(
        DbContext anyDbContext,
        string schemaPrefix,
        CancellationToken cancellationToken = default)
    {
        ValidatePrefix(schemaPrefix);

        var connection = anyDbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Per-gym tables carry cross-schema FKs (e.g. g{N}_scheduling.bookings
        // references g{N}_billing.session_packages). Reverse-module ordering on
        // its own is not enough — drop every FK in this gym's schemas first, then
        // tables, then schemas.
        var schemaInPredicate = string.Join(",", PerGymModuleSchemas.Select(m => $"N'{schemaPrefix}_{m}'"));

        using (var dropFksCmd = connection.CreateCommand())
        {
            dropFksCmd.CommandText = $@"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name)
                             + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + '; '
                FROM sys.foreign_keys fk
                JOIN sys.tables t  ON t.object_id = fk.parent_object_id
                JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name IN ({schemaInPredicate});
                EXEC sp_executesql @sql;";
            await dropFksCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var module in PerGymModuleSchemas.Reverse())
        {
            var schemaName = $"{schemaPrefix}_{module}";

            using var dropTablesCmd = connection.CreateCommand();
            dropTablesCmd.CommandText = $@"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'DROP TABLE [{schemaName}].[' + name + ']; '
                FROM sys.tables WHERE schema_id = SCHEMA_ID(N'{schemaName}');
                EXEC sp_executesql @sql;";
            await dropTablesCmd.ExecuteNonQueryAsync(cancellationToken);

            using var dropSchemaCmd = connection.CreateCommand();
            dropSchemaCmd.CommandText = $"IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schemaName}') EXEC('DROP SCHEMA [{schemaName}]');";
            await dropSchemaCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public static void ValidatePrefix(string schemaPrefix)
    {
        if (string.IsNullOrEmpty(schemaPrefix) || !PrefixPattern.IsMatch(schemaPrefix))
            throw new ArgumentException(
                $"Invalid schema prefix '{schemaPrefix}'. Must match ^g\\d+$ (e.g., g1, g42).",
                nameof(schemaPrefix));
    }
}
