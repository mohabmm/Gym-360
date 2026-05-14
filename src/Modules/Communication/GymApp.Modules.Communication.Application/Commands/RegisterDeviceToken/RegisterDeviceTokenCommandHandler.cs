using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Contracts;

namespace GymApp.Modules.Communication.Application.Commands.RegisterDeviceToken;

internal sealed class RegisterDeviceTokenCommandHandler : ICommandHandler<RegisterDeviceTokenCommand>
{
    private readonly IFcmTokenStore _fcmTokenStore;

    public RegisterDeviceTokenCommandHandler(IFcmTokenStore fcmTokenStore)
    {
        _fcmTokenStore = fcmTokenStore;
    }

    public async Task<Result> Handle(RegisterDeviceTokenCommand command, CancellationToken cancellationToken)
    {
        await _fcmTokenStore.RegisterTokenAsync(command.UserId, command.Token, command.Platform, cancellationToken);
        return Result.Success();
    }
}
