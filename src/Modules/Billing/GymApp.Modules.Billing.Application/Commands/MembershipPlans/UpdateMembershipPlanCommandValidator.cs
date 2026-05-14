using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.MembershipPlans;

internal sealed class UpdateMembershipPlanCommandValidator : AbstractValidator<UpdateMembershipPlanCommand>
{
    public UpdateMembershipPlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        When(x => x.DurationDays.HasValue, () =>
            RuleFor(x => x.DurationDays!.Value).GreaterThan(0).LessThanOrEqualTo(3650));
        When(x => x.Price.HasValue, () =>
            RuleFor(x => x.Price!.Value).GreaterThanOrEqualTo(0));
        When(x => !string.IsNullOrEmpty(x.Name), () =>
            RuleFor(x => x.Name!).MaximumLength(200));
    }
}
