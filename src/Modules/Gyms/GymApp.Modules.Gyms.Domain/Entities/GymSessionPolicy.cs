using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymSessionPolicy : Entity
{
    private GymSessionPolicy() : base() { }

    private GymSessionPolicy(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public int CancellationWindowHours { get; private set; }
    public int NoshowThreshold { get; private set; }
    public int NoshowWindowDays { get; private set; }
    public NoshowAction NoshowAction { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public static GymSessionPolicy Create(Guid gymId)
    {
        return new GymSessionPolicy(Guid.NewGuid())
        {
            GymId = gymId,
            CancellationWindowHours = 24,
            NoshowThreshold = 3,
            NoshowWindowDays = 30,
            NoshowAction = NoshowAction.FlagOnly
        };
    }

    public void Update(SessionPolicyRules rules, Guid updatedBy)
    {
        CancellationWindowHours = rules.CancellationWindowHours;
        NoshowThreshold = rules.NoshowThreshold;
        NoshowWindowDays = rules.NoshowWindowDays;
        NoshowAction = rules.NoshowAction;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
