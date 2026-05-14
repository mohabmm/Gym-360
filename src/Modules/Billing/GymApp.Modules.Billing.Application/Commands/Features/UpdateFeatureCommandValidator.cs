using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.Features;

internal sealed class UpdateFeatureCommandValidator : AbstractValidator<UpdateFeatureCommand>
{
    public UpdateFeatureCommandValidator()
    {
        RuleFor(x => x.FeatureId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
    }
}
