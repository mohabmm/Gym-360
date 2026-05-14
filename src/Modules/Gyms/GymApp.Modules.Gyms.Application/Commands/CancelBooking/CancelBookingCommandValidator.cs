using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.CancelBooking;

internal sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
