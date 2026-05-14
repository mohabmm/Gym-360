using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.UpdateMealPlan;

internal sealed class UpdateMealPlanCommandValidator : AbstractValidator<UpdateMealPlanCommand>
{
    public UpdateMealPlanCommandValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.DailyCalorieTarget).GreaterThan(0).When(x => x.DailyCalorieTarget.HasValue);
    }
}
