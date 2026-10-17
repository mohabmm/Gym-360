using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class TrainerEarningsRecord : Entity
{
    private TrainerEarningsRecord() : base() { }

    private TrainerEarningsRecord(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid MemberId { get; private set; }
    public decimal RateApplied { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public DateTime EarnedAt { get; private set; }

    public static TrainerEarningsRecord Create(TrainerEarningsDetails details)
    {
        return new TrainerEarningsRecord(Guid.NewGuid())
        {
            GymId = details.GymId,
            TrainerId = details.TrainerId,
            SessionId = details.SessionId,
            MemberId = details.MemberId,
            RateApplied = details.RateApplied,
            Currency = "EGP",
            EarnedAt = DateTime.UtcNow
        };
    }
}
