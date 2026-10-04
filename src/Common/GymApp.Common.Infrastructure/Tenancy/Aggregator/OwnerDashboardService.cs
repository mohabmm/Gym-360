using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GymApp.Common.Infrastructure.Tenancy.Aggregator;

/// <summary>
/// Cross-tenant rollup for an Owner: walks gyms_registry, then issues a single
/// dynamic UNION ALL across each gym's per-module schemas to roll up active
/// members, trainers, recent revenue, and recent sessions.
///
/// SchemaPrefix is regex-validated at registry insert (^g\d+$), so we splice
/// it into SQL safely. Cached in IDistributedCache for 5 minutes, scoped per
/// owner.
/// </summary>
internal sealed class OwnerDashboardService : IOwnerDashboardService
{
    private static readonly Regex SafePrefix = new(@"^g\d+$", RegexOptions.Compiled);
    private const int CacheTtlSeconds = 300;

    private readonly TenantRegistryDbContext _registry;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OwnerDashboardService> _logger;

    public OwnerDashboardService(
        TenantRegistryDbContext registry,
        IMemoryCache cache,
        ILogger<OwnerDashboardService> logger)
    {
        _registry = registry;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OwnerDashboard> GetAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"owner-dashboard:{ownerId}";
        if (_cache.TryGetValue<OwnerDashboard>(cacheKey, out var cached) && cached is not null)
            return cached;

        var gyms = await _registry.Gyms.AsNoTracking()
            .Where(g => g.OwnerId == ownerId && g.Status == "Active")
            .OrderBy(g => g.SequentialNumber)
            .ToListAsync(cancellationToken);

        if (gyms.Count == 0)
            return new OwnerDashboard(ownerId, 0, 0, 0, 0, 0, Array.Empty<OwnerDashboardGymRow>());

        // Defensive: every prefix must match `^g\d+$`.
        foreach (var gym in gyms)
        {
            if (!SafePrefix.IsMatch(gym.SchemaPrefix))
                throw new InvalidOperationException($"Refusing to query gym with unsafe prefix '{gym.SchemaPrefix}'.");
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Build a single batch with one row per gym; tolerate missing tables
        // (a freshly-provisioned gym may have no payments yet).
        var sql = new StringBuilder();
        for (int i = 0; i < gyms.Count; i++)
        {
            var gym = gyms[i];
            var p = gym.SchemaPrefix;
            if (i > 0) sql.AppendLine(" UNION ALL ");
            sql.AppendLine($@"
                SELECT
                    CAST('{gym.GymId}' AS UNIQUEIDENTIFIER) AS GymId,
                    N'{gym.Name.Replace("'", "''")}' AS GymName,
                    N'{p}' AS SchemaPrefix,
                    (SELECT COUNT(*) FROM [{p}_gyms].gym_members WHERE IsFrozen = 0) AS ActiveMembers,
                    (SELECT COUNT(DISTINCT m.UserId) FROM [identity].user_gym_memberships m WHERE m.is_active = 1 AND m.role = 'Trainer' AND m.gym_id = '{gym.GymId}') AS Trainers,
                    ISNULL((SELECT SUM(p.amount) FROM [{p}_billing].payments p WHERE p.PaidAt >= @cutoff), 0) AS RevenueLast30,
                    (SELECT COUNT(*) FROM [{p}_scheduling].sessions s WHERE s.ScheduledDate >= CAST(@cutoff AS DATE)) AS SessionsLast30
            ");
        }

        var rows = new List<OwnerDashboardGymRow>(gyms.Count);
        var connection = _registry.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql.ToString();
            var p = cmd.CreateParameter();
            p.ParameterName = "@cutoff";
            p.Value = thirtyDaysAgo;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new OwnerDashboardGymRow(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.GetDecimal(5),
                    reader.GetInt32(6)));
            }
        }
        catch (Exception ex)
        {
            // A single missing table (e.g. fresh gym before any tables created) shouldn't
            // tank the whole dashboard. Fall back to per-gym defaults.
            _logger.LogError(ex, "OwnerDashboardService: cross-gym rollup failed; returning per-gym empties");
            rows.Clear();
            foreach (var gym in gyms)
                rows.Add(new OwnerDashboardGymRow(gym.GymId, gym.Name, gym.SchemaPrefix, 0, 0, 0, 0));
        }

        var dashboard = new OwnerDashboard(
            OwnerId: ownerId,
            TotalGyms: rows.Count,
            TotalActiveMembers: rows.Sum(r => r.ActiveMembers),
            TotalTrainers: rows.Sum(r => r.Trainers),
            TotalRevenueLast30Days: rows.Sum(r => r.RevenueLast30Days),
            TotalSessionsLast30Days: rows.Sum(r => r.SessionsLast30Days),
            ByGym: rows);

        _cache.Set(cacheKey, dashboard, TimeSpan.FromSeconds(CacheTtlSeconds));
        return dashboard;
    }
}
