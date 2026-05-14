using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.LogWater;

internal sealed class LogWaterCommandValidator : AbstractValidator<LogWaterCommand>
{
    public LogWaterCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.AmountMl).GreaterThan(0).WithMessage("Water amount must be greater than zero.");
    }
}
