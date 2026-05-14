using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateOperatingHours;

internal sealed class UpdateOperatingHoursCommandValidator : AbstractValidator<UpdateOperatingHoursCommand>
{
    public UpdateOperatingHoursCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Hours).NotNull();
        RuleForEach(x => x.Hours).ChildRules(h =>
        {
            h.RuleFor(d => d.DayOfWeek).NotEmpty();
            h.RuleFor(d => d.OpenTime).NotEmpty();
            h.RuleFor(d => d.CloseTime).NotEmpty();
        });
    }
}
