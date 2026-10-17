using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class Contract : Entity
{
    private Contract() { }

    private Contract(Guid id) : base(id) { }

    public Guid OwnerId { get; private set; }
    public DashboardMode DashboardMode { get; private set; }
    public string PlanTier { get; private set; } = default!;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    public static Contract Create(Guid ownerId, ContractDetails details)
    {
        return new Contract(Guid.NewGuid())
        {
            OwnerId = ownerId,
            DashboardMode = details.DashboardMode,
            PlanTier = details.PlanTier,
            EffectiveFrom = details.EffectiveFrom,
            EffectiveTo = details.EffectiveTo
        };
    }

    public void ChangeDashboardMode(DashboardMode mode) => DashboardMode = mode;

    public void ChangePlanTier(string planTier) => PlanTier = planTier;

    public void End(DateTime effectiveTo) => EffectiveTo = effectiveTo;
}
