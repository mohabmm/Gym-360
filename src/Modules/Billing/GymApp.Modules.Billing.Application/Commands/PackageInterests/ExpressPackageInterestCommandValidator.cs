using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

internal sealed class ExpressPackageInterestCommandValidator : AbstractValidator<ExpressPackageInterestCommand>
{
    public ExpressPackageInterestCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.SourcePackageId).NotEmpty();
    }
}
