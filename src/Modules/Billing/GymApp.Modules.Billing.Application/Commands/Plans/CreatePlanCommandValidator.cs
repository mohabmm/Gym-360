using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

internal sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AnnualPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MemberLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TrainerLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StaffLimit).GreaterThanOrEqualTo(0);
    }
}
