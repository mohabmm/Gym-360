using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    Guid UserId,
    string Token,
    string Platform) : ICommand;
