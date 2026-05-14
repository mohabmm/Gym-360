using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gamification.Application.Commands.CalculateMonthlyTrainerScores;

public sealed record CalculateMonthlyTrainerScoresCommand(
    Guid GymId,
    int Month,
    int Year) : ICommand;
