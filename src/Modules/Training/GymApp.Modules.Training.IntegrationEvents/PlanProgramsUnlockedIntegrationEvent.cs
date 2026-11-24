using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

/// <summary>
/// pt.starter_plan: the staff let <paramref name="TrainerId"/> give <paramref name="TraineeId"/> one
/// program of <paramref name="LengthDays"/> days (7 or 30) from the templates and ready programs.
/// </summary>
public sealed record PlanProgramsUnlockedIntegrationEvent(
    Guid GymId,
    Guid TrainerId,
    Guid TraineeId,
    Guid UnlockedBy,
    int LengthDays = 7) : IIntegrationEvent;
