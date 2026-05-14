using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymSessionPolicy : Entity
{
    private GymSessionPolicy() : base() { }

    private GymSessionPolicy(
        Guid id,
        Guid gymId,
        int cancellationWindowHours,
        int noshowThreshold,
        int noshowWindowDays,
        NoshowAction noshowAction) : base(id)
    {
        GymId = gymId;
        CancellationWindowHours = cancellationWindowHours;
        NoshowThreshold = noshowThreshold;
        NoshowWindowDays = noshowWindowDays;
        NoshowAction = noshowAction;
    }

    public Guid GymId { get; private set; }
    public int CancellationWindowHours { get; private set; }
    public int NoshowThreshold { get; private set; }
    public int NoshowWindowDays { get; private set; }
    public NoshowAction NoshowAction { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public static GymSessionPolicy Create(Guid gymId)
    {
        return new GymSessionPolicy(
            Guid.NewGuid(),
            gymId,
            cancellationWindowHours: 24,
            noshowThreshold: 3,
            noshowWindowDays: 30,
            noshowAction: NoshowAction.FlagOnly);
    }

    public void Update(
        int cancellationWindowHours,
        int noshowThreshold,
        int noshowWindowDays,
        NoshowAction noshowAction,
        Guid updatedBy)
    {
        CancellationWindowHours = cancellationWindowHours;
        NoshowThreshold = noshowThreshold;
        NoshowWindowDays = noshowWindowDays;
        NoshowAction = noshowAction;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
