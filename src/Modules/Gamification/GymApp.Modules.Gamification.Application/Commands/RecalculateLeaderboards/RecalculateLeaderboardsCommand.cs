using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gamification.Application.Commands.RecalculateLeaderboards;

public sealed record RecalculateLeaderboardsCommand(Guid GymId) : ICommand;
