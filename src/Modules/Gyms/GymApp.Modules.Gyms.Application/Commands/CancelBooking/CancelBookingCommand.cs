using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.CancelBooking;

public sealed record CancelBookingCommand(
    Guid GymId,
    Guid ClassId,
    Guid BookingId) : ICommand;
