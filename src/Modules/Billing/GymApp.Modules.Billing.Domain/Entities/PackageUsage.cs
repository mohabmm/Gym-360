using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Enums;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class PackageUsage : Entity
{
    private PackageUsage() { }

    public Guid SessionPackageId { get; private set; }
    public Guid SessionId { get; private set; }
    public DateTime UsedAt { get; private set; }
    public PackageUsageType UsageType { get; private set; }

    internal static PackageUsage Create(
        Guid sessionPackageId,
        Guid sessionId,
        PackageUsageType usageType)
    {
        return new PackageUsage
        {
            Id = Guid.NewGuid(),
            SessionPackageId = sessionPackageId,
            SessionId = sessionId,
            UsedAt = DateTime.UtcNow,
            UsageType = usageType
        };
    }
}
