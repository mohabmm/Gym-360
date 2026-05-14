using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;

internal sealed class UpdateSeasonalHoursCommandValidator : AbstractValidator<UpdateSeasonalHoursCommand>
{
    public UpdateSeasonalHoursCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Overrides).NotNull();
        RuleForEach(x => x.Overrides).ChildRules(o =>
        {
            o.RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
            o.RuleFor(v => v.StartDate).NotEmpty();
            o.RuleFor(v => v.EndDate).NotEmpty();
        });
    }
}
