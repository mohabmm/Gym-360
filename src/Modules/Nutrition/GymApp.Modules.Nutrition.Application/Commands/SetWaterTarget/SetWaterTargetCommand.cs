using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Nutrition.Application.Commands.SetWaterTarget;

public sealed record SetWaterTargetCommand(
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    int TargetMl) : ICommand;
