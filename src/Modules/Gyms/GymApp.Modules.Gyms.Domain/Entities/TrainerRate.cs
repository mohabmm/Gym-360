using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Events;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class TrainerRate : Entity
{
    private TrainerRate() : base() { }

    private TrainerRate(
        Guid id,
        Guid gymId,
        Guid trainerId,
        decimal ratePerSession,
        Guid setBy) : base(id)
    {
        GymId = gymId;
        TrainerId = trainerId;
        RatePerSession = ratePerSession;
        Currency = "EGP";
        EffectiveFrom = DateTime.UtcNow;
        SetBy = setBy;
    }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public decimal RatePerSession { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public DateTime EffectiveFrom { get; private set; }
    public Guid SetBy { get; private set; }

    public static TrainerRate Create(Guid gymId, Guid trainerId, decimal rate, Guid setBy)
    {
        var trainerRate = new TrainerRate(Guid.NewGuid(), gymId, trainerId, rate, setBy);
        trainerRate.RaiseDomainEvent(new TrainerRateSetDomainEvent(gymId, trainerId, rate));
        return trainerRate;
    }

    public void Update(decimal rate, Guid setBy)
    {
        RatePerSession = rate;
        EffectiveFrom = DateTime.UtcNow;
        SetBy = setBy;
        RaiseDomainEvent(new TrainerRateSetDomainEvent(GymId, TrainerId, rate));
    }
}
