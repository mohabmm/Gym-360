using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.BookClass;

internal sealed class BookClassCommandValidator : AbstractValidator<BookClassCommand>
{
    public BookClassCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
