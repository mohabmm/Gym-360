using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.LogFood;

internal sealed class LogFoodCommandValidator : AbstractValidator<LogFoodCommand>
{
    public LogFoodCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MealType).NotEmpty();
        RuleFor(x => x.FoodItems).NotEmpty().WithMessage("At least one food item is required.");

        RuleForEach(x => x.FoodItems).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.Unit).NotEmpty();
            item.RuleFor(x => x.Calories).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.Protein).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.Carbs).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.Fat).GreaterThanOrEqualTo(0);
        });
    }
}
