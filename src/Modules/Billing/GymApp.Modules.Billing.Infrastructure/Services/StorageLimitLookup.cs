using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Infrastructure.Services;

/// <summary>Resolves a plan's storage limits by resource type, falling back to the platform defaults.</summary>
internal sealed class StorageLimitLookup
{
    public const long DefaultStorageLimitBytes = 100L * 1024 * 1024; // 100MB default

    private readonly List<PlanStorageLimit> _limits;

    public StorageLimitLookup(List<PlanStorageLimit> limits) => _limits = limits;

    public long TotalStorageBytes => Find("total_storage")?.LimitValue ?? DefaultStorageLimitBytes;

    public int PhotoLimitPerMember => (int?)Find("progress_photos_per_member")?.LimitValue ?? 5;

    public int CheckInRetentionDays => (int?)Find("checkin_retention_days")?.LimitValue ?? 30;

    private PlanStorageLimit? Find(string resourceType) =>
        _limits.FirstOrDefault(sl => sl.ResourceType == resourceType);
}
