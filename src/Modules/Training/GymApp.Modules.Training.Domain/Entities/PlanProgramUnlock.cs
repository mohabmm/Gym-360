using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.starter_plan: whether the staff let one coach give one member a one-week or 30-day
/// program (<see cref="Length"/>) from the templates and ready programs (e.g. the member stopped coming). Until then the coach may give
/// only a 3-day starter plan (<see cref="ValueObjects.PlanLimitRules"/>). The unlock is used up by
/// the plan it allows (<see cref="Lock"/> by the coach). One row per coach–member pair;
/// locking keeps the row so the desk can see who changed it last.
/// </summary>
public sealed class PlanProgramUnlock : Entity
{
    private PlanProgramUnlock() { }

    private PlanProgramUnlock(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid TraineeId { get; private set; }
    /// <summary>The program length the staff chose last.</summary>
    public UnlockLength Length { get; private set; }
    public DateTime UnlockedAt { get; private set; }
    public Guid UnlockedBy { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public Guid? LockedBy { get; private set; }

    public bool IsUnlocked => LockedAt is null;

    public static PlanProgramUnlock Grant(CoachTrainee pair, UnlockLength length, Guid by) => new(Guid.NewGuid())
    {
        GymId = pair.GymId,
        TrainerId = pair.TrainerId,
        TraineeId = pair.TraineeId,
        Length = length,
        UnlockedAt = DateTime.UtcNow,
        UnlockedBy = by
    };

    /// <summary>Opens a program of <paramref name="length"/> again, or changes the length. False when nothing changed.</summary>
    public bool Unlock(UnlockLength length, Guid by)
    {
        if (IsUnlocked && Length == length)
            return false;
        Length = length;
        UnlockedAt = DateTime.UtcNow;
        UnlockedBy = by;
        LockedAt = null;
        LockedBy = null;
        return true;
    }

    /// <summary>Closed again — by the staff, or by the coach giving the one-week program. False when already locked.</summary>
    public bool Lock(Guid by)
    {
        if (!IsUnlocked)
            return false;
        LockedAt = DateTime.UtcNow;
        LockedBy = by;
        return true;
    }
}

/// <summary>One coach and one of their members in a gym.</summary>
public sealed record CoachTrainee(Guid GymId, Guid TrainerId, Guid TraineeId);
