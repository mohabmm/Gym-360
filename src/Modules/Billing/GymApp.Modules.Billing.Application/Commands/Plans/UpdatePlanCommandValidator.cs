using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

internal sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0).When(x => x.MonthlyPrice.HasValue);
        RuleFor(x => x.AnnualPrice).GreaterThanOrEqualTo(0).When(x => x.AnnualPrice.HasValue);
        RuleFor(x => x.MemberLimit).GreaterThanOrEqualTo(0).When(x => x.MemberLimit.HasValue);
        RuleFor(x => x.TrainerLimit).GreaterThanOrEqualTo(0).When(x => x.TrainerLimit.HasValue);
        RuleFor(x => x.StaffLimit).GreaterThanOrEqualTo(0).When(x => x.StaffLimit.HasValue);
    }
}
