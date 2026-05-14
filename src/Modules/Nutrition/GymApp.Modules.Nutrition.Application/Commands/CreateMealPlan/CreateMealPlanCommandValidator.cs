using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.CreateMealPlan;

internal sealed class CreateMealPlanCommandValidator : AbstractValidator<CreateMealPlanCommand>
{
    public CreateMealPlanCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DailyCalorieTarget).GreaterThan(0);

        RuleFor(x => x.MacroTargets).NotNull();
        RuleFor(x => x.MacroTargets.ProteinGrams).GreaterThanOrEqualTo(0)
            .When(x => x.MacroTargets is not null);
        RuleFor(x => x.MacroTargets.CarbsGrams).GreaterThanOrEqualTo(0)
            .When(x => x.MacroTargets is not null);
        RuleFor(x => x.MacroTargets.FatGrams).GreaterThanOrEqualTo(0)
            .When(x => x.MacroTargets is not null);

        RuleFor(x => x.Days).NotNull();
    }
}
