using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gamification.Application.Commands.ToggleLeaderboardOptOut;

public sealed record ToggleLeaderboardOptOutCommand(
    Guid UserId,
    Guid GymId) : ICommand;
