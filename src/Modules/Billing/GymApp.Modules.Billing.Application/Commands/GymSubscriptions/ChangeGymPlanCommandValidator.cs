using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.GymSubscriptions;

internal sealed class ChangeGymPlanCommandValidator : AbstractValidator<ChangeGymPlanCommand>
{
    public ChangeGymPlanCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ChangedBy).NotEmpty();
    }
}
