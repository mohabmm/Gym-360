namespace GymApp.Modules.Billing.Infrastructure.Services;

/// <summary>
/// Cached gym feature set — stored in Redis with TTL 5 min.
/// </summary>
internal sealed class GymFeatureSet
{
    public Guid GymId { get; init; }
    public string PlanCode { get; init; } = "free";
    public List<string> EnabledFeatures { get; init; } = new();
    public int MemberLimit { get; init; }
    public int TrainerLimit { get; init; }
    public int StaffLimit { get; init; }
    public long StorageLimitBytes { get; init; }
    public int PhotoLimitPerMember { get; init; }
    public int CheckInRetentionDays { get; init; }

    public bool IsEnabled(string featureCode) => EnabledFeatures.Contains(featureCode);
}
