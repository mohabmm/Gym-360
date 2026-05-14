using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class AssignSessionPackageCommandValidator : AbstractValidator<AssignSessionPackageCommand>
{
    public AssignSessionPackageCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.AssignedBy).NotEmpty();
    }
}
