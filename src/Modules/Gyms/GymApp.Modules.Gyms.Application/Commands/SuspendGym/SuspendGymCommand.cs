using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.SuspendGym;

public sealed record SuspendGymCommand(
    Guid GymId,
    string Reason) : ICommand;
