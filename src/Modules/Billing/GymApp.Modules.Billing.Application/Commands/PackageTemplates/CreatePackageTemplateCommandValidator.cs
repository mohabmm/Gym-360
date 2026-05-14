using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

internal sealed class CreatePackageTemplateCommandValidator : AbstractValidator<CreatePackageTemplateCommand>
{
    public CreatePackageTemplateCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SessionCount).GreaterThan(0);
        RuleFor(x => x.PricePerSession).GreaterThan(0);
    }
}
