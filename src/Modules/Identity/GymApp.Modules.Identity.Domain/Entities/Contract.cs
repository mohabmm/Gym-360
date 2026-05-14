using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class Contract : Entity
{
    private Contract() { }

    private Contract(
        Guid id,
        Guid ownerId,
        DashboardMode dashboardMode,
        string planTier,
        DateTime effectiveFrom,
        DateTime? effectiveTo) : base(id)
    {
        OwnerId = ownerId;
        DashboardMode = dashboardMode;
        PlanTier = planTier;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid OwnerId { get; private set; }
    public DashboardMode DashboardMode { get; private set; }
    public string PlanTier { get; private set; } = default!;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    public static Contract Create(
        Guid ownerId,
        DashboardMode dashboardMode,
        string planTier,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
    {
        return new Contract(Guid.NewGuid(), ownerId, dashboardMode, planTier, effectiveFrom, effectiveTo);
    }

    public void ChangeDashboardMode(DashboardMode mode) => DashboardMode = mode;

    public void ChangePlanTier(string planTier) => PlanTier = planTier;

    public void End(DateTime effectiveTo) => EffectiveTo = effectiveTo;
}
