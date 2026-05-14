using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.ExportMyData;

internal sealed class ExportMyDataCommandValidator : AbstractValidator<ExportMyDataCommand>
{
    public ExportMyDataCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
