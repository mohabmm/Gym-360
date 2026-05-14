using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.ClearKioskPin;

public sealed record ClearKioskPinCommand(
    Guid GymId,
    string CurrentPin) : ICommand;
