using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

internal sealed class BulkUpdatePlanFeaturesCommandValidator : AbstractValidator<BulkUpdatePlanFeaturesCommand>
{
    public BulkUpdatePlanFeaturesCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Features).NotEmpty();
        RuleForEach(x => x.Features).ChildRules(feature =>
        {
            feature.RuleFor(x => x.FeatureId).NotEmpty();
        });
    }
}
