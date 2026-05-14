using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.SetTrainerRate;

public sealed record SetTrainerRateCommand(
    Guid GymId,
    Guid TrainerId,
    decimal RatePerSession,
    Guid SetByUserId) : ICommand;
