using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.CreateClass;

internal sealed class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Schedule).NotNull();
        RuleFor(x => x.Schedule.DurationMinutes).GreaterThan(0).When(x => x.Schedule is not null);
        RuleFor(x => x.Capacity).GreaterThan(0).LessThanOrEqualTo(500);
    }
}
