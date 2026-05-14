using FluentValidation;
using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Application.Commands.RegisterDeviceToken;

internal sealed class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    private static readonly string[] ValidPlatforms =
        Enum.GetNames<DevicePlatform>();

    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(p => ValidPlatforms.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Platform must be one of: {string.Join(", ", ValidPlatforms)}");
    }
}
