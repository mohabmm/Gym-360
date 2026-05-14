using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class PlanFeature : Entity
{
    private PlanFeature() { }

    public Guid PlanId { get; private set; }
    public Guid FeatureId { get; private set; }
    public bool IsEnabled { get; private set; }
    public int? LimitValue { get; private set; }

    public static PlanFeature Create(
        Guid planId,
        Guid featureId,
        bool isEnabled,
        int? limitValue = null)
    {
        return new PlanFeature
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            FeatureId = featureId,
            IsEnabled = isEnabled,
            LimitValue = limitValue
        };
    }

    public void Update(bool isEnabled, int? limitValue)
    {
        IsEnabled = isEnabled;
        LimitValue = limitValue;
    }
}
