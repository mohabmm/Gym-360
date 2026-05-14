using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Nutrition.Application.Commands.LogWater;

public sealed record LogWaterCommand(
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    int AmountMl) : ICommand<Guid>;
