using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.training_plan: one row per gym, set by the owner or a manager. A gym without a row uses
/// the defaults (<see cref="ForGym"/>); the row is created on the first change.
/// </summary>
public sealed class TrainingPlanSettings : Entity
{
    public const int DefaultInactiveDays = 5;
    public const int MinInactiveDays = 2;
    public const int MaxInactiveDays = 30;

    private TrainingPlanSettings() { }

    private TrainingPlanSettings(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    /// <summary>A trainee with a plan and no workout for this many days shows as "needs a nudge".</summary>
    public int InactiveDays { get; private set; } = DefaultInactiveDays;
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public static TrainingPlanSettings ForGym(Guid gymId) => new(Guid.NewGuid()) { GymId = gymId, UpdatedAt = DateTime.UtcNow };

    public Result SetInactiveDays(int days, Guid updatedBy)
    {
        if (days is < MinInactiveDays or > MaxInactiveDays)
            return Result.Failure(TrainingPlanErrors.InvalidSettings($"Inactive days must be {MinInactiveDays} to {MaxInactiveDays}."));
        InactiveDays = days;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
