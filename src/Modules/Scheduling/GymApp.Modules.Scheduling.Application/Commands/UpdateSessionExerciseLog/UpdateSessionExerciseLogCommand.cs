using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Commands.UpdateSessionExerciseLog;

public sealed record UpdateSessionExerciseLogCommand(
    Guid SessionId,
    Guid LogId,
    Guid TrainerId,
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes,
    ExerciseTarget? Target = null) : ICommand;
