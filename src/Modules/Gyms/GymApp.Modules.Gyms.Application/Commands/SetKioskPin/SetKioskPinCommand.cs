using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.SetKioskPin;

public sealed record SetKioskPinCommand(
    Guid GymId,
    string NewPin,
    string? CurrentPin) : ICommand;
