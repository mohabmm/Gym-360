using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

internal sealed class SetGymFeatureOverrideCommandValidator : AbstractValidator<SetGymFeatureOverrideCommand>
{
    public SetGymFeatureOverrideCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x).Must(c => c.FeatureId != Guid.Empty || !string.IsNullOrWhiteSpace(c.FeatureCode))
            .WithMessage("Either FeatureId or FeatureCode is required.")
            .WithName("Feature");
        RuleFor(x => x.SetBy).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
    }
}
