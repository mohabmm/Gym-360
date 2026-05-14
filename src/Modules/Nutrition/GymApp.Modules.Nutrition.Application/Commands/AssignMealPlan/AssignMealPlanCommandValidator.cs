using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.AssignMealPlan;

internal sealed class AssignMealPlanCommandValidator : AbstractValidator<AssignMealPlanCommand>
{
    public AssignMealPlanCommandValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
    }
}
