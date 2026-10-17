using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class GymFeatureOverride : Entity
{
    private GymFeatureOverride() { }

    public Guid GymId { get; private set; }
    public Guid FeatureId { get; private set; }
    public bool OverrideValue { get; private set; }
    public string? Reason { get; private set; }
    public Guid SetBy { get; private set; }
    public DateTime SetAt { get; private set; }

    public static GymFeatureOverride Create(GymFeatureOverrideDetails details)
    {
        return new GymFeatureOverride
        {
            Id = Guid.NewGuid(),
            GymId = details.GymId,
            FeatureId = details.FeatureId,
            OverrideValue = details.OverrideValue,
            Reason = details.Reason,
            SetBy = details.SetBy,
            SetAt = DateTime.UtcNow
        };
    }

    public void Update(bool overrideValue, string? reason, Guid setBy)
    {
        OverrideValue = overrideValue;
        Reason = reason;
        SetBy = setBy;
        SetAt = DateTime.UtcNow;
    }
}
