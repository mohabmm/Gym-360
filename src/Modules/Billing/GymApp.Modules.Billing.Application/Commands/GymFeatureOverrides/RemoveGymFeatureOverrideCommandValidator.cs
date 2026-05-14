using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

internal sealed class RemoveGymFeatureOverrideCommandValidator : AbstractValidator<RemoveGymFeatureOverrideCommand>
{
    public RemoveGymFeatureOverrideCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.FeatureId).NotEmpty();
    }
}
