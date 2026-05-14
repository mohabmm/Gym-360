using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

internal sealed class UpdatePackageTemplateCommandValidator : AbstractValidator<UpdatePackageTemplateCommand>
{
    public UpdatePackageTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        When(x => x.SessionCount.HasValue, () =>
            RuleFor(x => x.SessionCount!.Value).GreaterThan(0).LessThanOrEqualTo(1000));
        When(x => x.PricePerSession.HasValue, () =>
            RuleFor(x => x.PricePerSession!.Value).GreaterThanOrEqualTo(0));
        When(x => !string.IsNullOrEmpty(x.Name), () =>
            RuleFor(x => x.Name!).MaximumLength(200));
    }
}
