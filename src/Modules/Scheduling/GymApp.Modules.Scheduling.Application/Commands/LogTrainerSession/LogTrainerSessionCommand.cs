using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.LogTrainerSession;

public sealed record LogTrainerSessionCommand(
    Guid SessionId,
    Guid TrainerId,
    string ExercisesPerformed,
    string? ActualVsPlanned,
    int ClientEffortRating,
    string? Feedback,
    string? TechniqueNotes,
    string? AreasToImprove,
    string? NextSessionFocus,
    bool AttendanceConfirmed) : ICommand;
