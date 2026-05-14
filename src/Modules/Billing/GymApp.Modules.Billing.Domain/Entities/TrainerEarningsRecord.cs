using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class TrainerEarningsRecord : Entity
{
    private TrainerEarningsRecord() : base() { }

    private TrainerEarningsRecord(
        Guid id,
        Guid gymId,
        Guid trainerId,
        Guid sessionId,
        Guid memberId,
        decimal rateApplied) : base(id)
    {
        GymId = gymId;
        TrainerId = trainerId;
        SessionId = sessionId;
        MemberId = memberId;
        RateApplied = rateApplied;
        Currency = "EGP";
        EarnedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid MemberId { get; private set; }
    public decimal RateApplied { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public DateTime EarnedAt { get; private set; }

    public static TrainerEarningsRecord Create(
        Guid gymId,
        Guid trainerId,
        Guid sessionId,
        Guid memberId,
        decimal rateApplied)
    {
        return new TrainerEarningsRecord(
            Guid.NewGuid(),
            gymId,
            trainerId,
            sessionId,
            memberId,
            rateApplied);
    }
}
