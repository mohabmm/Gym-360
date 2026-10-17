using System.Data;
using System.Text.RegularExpressions;
using GymApp.Common.Application.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Common.Infrastructure.Tenancy.Registry;

internal sealed class GymRegistryService : IGymRegistry
{
    private static readonly Regex SafePrefix = new(@"^g\d+$", RegexOptions.Compiled);

    private readonly TenantRegistryDbContext _db;

    public GymRegistryService(TenantRegistryDbContext db)
    {
        _db = db;
    }

    public Task<GymRegistration?> FindAsync(Guid gymId, CancellationToken cancellationToken = default)
        => _db.Gyms.AsNoTracking().FirstOrDefaultAsync(g => g.GymId == gymId, cancellationToken);

    public async Task<GymRegistration?> FindByCodeAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gymCode)) return null;
        var trimmed = gymCode.Trim().ToUpperInvariant();

        // Fast path: indexed lookup on the denormalized GymCode column. Startup backfill
        // populates legacy rows; the walker below handles any row the backfill missed
        // (e.g. provisioning race).
        var fast = await _db.Gyms.AsNoTracking()
            .FirstOrDefaultAsync(g => g.GymCode == trimmed && g.Status == "Active", cancellationToken);
        if (fast is not null) return fast;

        return await FindByCodeInTenantSchemasAsync(trimmed, cancellationToken);
    }

    // Fallback walker for rows missing the denormalized GymCode column. SchemaPrefix
    // is regex-validated before splicing into SQL.
    private async Task<GymRegistration?> FindByCodeInTenantSchemasAsync(string code, CancellationToken cancellationToken)
    {
        var actives = await _db.Gyms.AsNoTracking()
            .Where(g => g.Status == "Active" && g.GymCode == null)
            .OrderBy(g => g.SequentialNumber)
            .ToListAsync(cancellationToken);

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        foreach (var gym in actives.Where(g => SafePrefix.IsMatch(g.SchemaPrefix)))
        {
            if (await TenantSchemaHasCodeAsync(conn, gym.SchemaPrefix, code, cancellationToken))
                return gym;
        }

        return null;
    }

    private static async Task<bool> TenantSchemaHasCodeAsync(
        System.Data.Common.DbConnection conn, string schemaPrefix, string code, CancellationToken cancellationToken)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT TOP 1 1 FROM [{schemaPrefix}_gyms].gyms WHERE UPPER(GymCode) = @code";
        var p = cmd.CreateParameter();
        p.ParameterName = "@code";
        p.Value = code;
        cmd.Parameters.Add(p);

        try
        {
            return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
        }
        catch (SqlException)
        {
            // Newly provisioned gyms may not have the table yet; skip.
            return false;
        }
    }

    public async Task<IReadOnlyList<GymRegistration>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
        => await _db.Gyms.AsNoTracking()
            .Where(g => g.OwnerId == ownerId && g.Status == "Active")
            .OrderBy(g => g.SequentialNumber)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GymRegistration>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _db.Gyms.AsNoTracking()
            .Where(g => g.Status == "Active")
            .OrderBy(g => g.SequentialNumber)
            .ToListAsync(cancellationToken);
}
