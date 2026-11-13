using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.SetProgressGoal;

/// <summary>Sets the member's goal. TrainerId set = a coach setting it for a client.</summary>
public sealed record SetProgressGoalCommand(
    Guid MemberId,
    Guid GymId,
    Guid? TrainerId,
    string Goal,
    decimal? TargetWeight) : ICommand<ProgressGoalDto>;
