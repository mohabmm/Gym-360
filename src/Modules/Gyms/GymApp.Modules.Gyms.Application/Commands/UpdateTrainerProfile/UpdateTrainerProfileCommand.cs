using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateTrainerProfile;

public sealed record UpdateTrainerProfileCommand(
    Guid GymId,
    Guid TrainerId,
    string? Bio,
    IReadOnlyList<string>? Specializations) : ICommand;
