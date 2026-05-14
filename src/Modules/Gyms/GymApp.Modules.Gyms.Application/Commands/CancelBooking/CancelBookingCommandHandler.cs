using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.CancelBooking;

internal sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand>
{
    private readonly IGymClassRepository _gymClassRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public CancelBookingCommandHandler(IGymClassRepository gymClassRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymClassRepository = gymClassRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var gymClass = await _gymClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (gymClass is null || gymClass.GymId != request.GymId)
            return Result.Failure(GymErrors.ClassNotFound);

        var booking = gymClass.Bookings.FirstOrDefault(b => b.Id == request.BookingId);
        if (booking is null)
            return Result.Failure(GymErrors.BookingNotFound);

        var result = booking.Cancel();
        if (result.IsFailure)
            return result;

        _gymClassRepository.Update(gymClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
