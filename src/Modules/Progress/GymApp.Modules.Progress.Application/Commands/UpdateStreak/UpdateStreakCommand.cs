using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Progress.Application.Commands.UpdateStreak;

public sealed record UpdateStreakCommand(
    Guid UserId,
    Guid GymId,
    string StreakType,
    DateOnly ActivityDate) : ICommand;
