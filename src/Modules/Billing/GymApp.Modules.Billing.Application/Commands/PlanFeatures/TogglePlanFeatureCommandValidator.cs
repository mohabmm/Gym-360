using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

internal sealed class TogglePlanFeatureCommandValidator : AbstractValidator<TogglePlanFeatureCommand>
{
    public TogglePlanFeatureCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.FeatureId).NotEmpty();
    }
}
