using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.DeleteFoodItem;

internal sealed class DeleteFoodItemCommandValidator : AbstractValidator<DeleteFoodItemCommand>
{
    public DeleteFoodItemCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MealEntryId).NotEmpty();
        RuleFor(x => x.ItemIndex).GreaterThanOrEqualTo(0);
    }
}
