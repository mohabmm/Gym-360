using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.RecordConsent;

internal sealed class RecordConsentCommandValidator : AbstractValidator<RecordConsentCommand>
{
    private static readonly string[] AllowedConsentTypes =
    {
        "DataProcessing",
        "Marketing",
        "Analytics",
        "ThirdPartySharing",
        "HealthDataProcessing",
        "LeaderboardVisibility"
    };

    public RecordConsentCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ConsentType)
            .NotEmpty().WithMessage("Consent type is required.")
            .Must(type => AllowedConsentTypes.Contains(type))
            .WithMessage($"Consent type must be one of: {string.Join(", ", AllowedConsentTypes)}.");

        RuleFor(x => x.ConsentText)
            .NotEmpty().WithMessage("Consent text is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}
