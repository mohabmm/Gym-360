using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.AddTrainer;

public sealed record AddTrainerCommand(
    Guid GymId,
    Guid TrainerId,
    string? Bio = null,
    IReadOnlyList<string>? Specializations = null) : ICommand;
