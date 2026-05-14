using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class BlockedDate : Entity
{
    private BlockedDate() : base() { }

    private BlockedDate(
        Guid id,
        Guid trainerAvailabilityId,
        DateOnly date,
        string? reason,
        bool autoNotifyClients) : base(id)
    {
        TrainerAvailabilityId = trainerAvailabilityId;
        Date = date;
        Reason = reason;
        AutoNotifyClients = autoNotifyClients;
    }

    public Guid TrainerAvailabilityId { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Reason { get; private set; }
    public bool AutoNotifyClients { get; private set; }

    public static BlockedDate Create(
        Guid trainerAvailabilityId,
        DateOnly date,
        string? reason,
        bool autoNotifyClients = false)
    {
        return new BlockedDate(
            Guid.NewGuid(),
            trainerAvailabilityId,
            date,
            reason,
            autoNotifyClients);
    }
}
