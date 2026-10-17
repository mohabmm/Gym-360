using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.BookSession;

internal sealed class BookSessionCommandHandler : ICommandHandler<BookSessionCommand, Guid>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly ISessionPackageService _sessionPackageService;

    public BookSessionCommandHandler(
        ISessionRepository sessionRepository,
        ITrainerAvailabilityRepository availabilityRepository,
        ISchedulingUnitOfWork unitOfWork,
        ISessionPackageService sessionPackageService)
    {
        _sessionRepository = sessionRepository;
        _availabilityRepository = availabilityRepository;
        _unitOfWork = unitOfWork;
        _sessionPackageService = sessionPackageService;
    }

    public async Task<Result<Guid>> Handle(BookSessionCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SessionType>(request.SessionType, true, out var sessionType))
            return Result.Failure<Guid>(new Error("Scheduling.InvalidSessionType", "The provided session type is not valid."));

        var packageCheck = await EnsurePackageNotExpiredAsync(request.SessionPackageId, cancellationToken);
        if (packageCheck.IsFailure)
            return Result.Failure<Guid>(packageCheck.Error);

        // Capacity guard: reject when sum(active package remaining) <= count of
        // the member's already-pending sessions at this gym. This catches the
        // case where the trainer has not accepted earlier reservations yet —
        // each Pending booking still occupies a future slot from the package.
        var bookable = await _sessionPackageService.GetBookableSessionsAsync(
            request.MemberId, request.GymId, cancellationToken);
        if (bookable <= 0)
            return Result.Failure<Guid>(new Error(
                "SESSION_PACKAGE.NO_CAPACITY",
                "You have reached your reserved-session capacity. Cancel a pending booking or wait for one to complete."));

        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (availability is null)
            return Result.Failure<Guid>(SchedulingErrors.AvailabilityNotFoundForTrainer(request.TrainerId));

        if (!IsBookable(availability, request))
            return Result.Failure<Guid>(SchedulingErrors.SlotNotAvailable);

        // Check for existing bookings at this time
        var existingSessions = await _sessionRepository.GetByDateAndTrainerAsync(
            request.TrainerId, request.Date, cancellationToken);

        var window = new BookingWindow(request.Time, request.DurationMinutes, availability.BufferMinutes);
        if (window.ConflictsWithAny(existingSessions))
            return Result.Failure<Guid>(SchedulingErrors.SessionAlreadyBooked);

        var session = Session.Create(
            new SessionParticipants(request.GymId, request.TrainerId, request.MemberId),
            new SessionSchedule(request.Date, request.Time, request.DurationMinutes),
            sessionType,
            request.Focus);

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session.Id);
    }

    // Guard: if a session package is provided, ensure it has not expired
    private async Task<Result> EnsurePackageNotExpiredAsync(Guid? sessionPackageId, CancellationToken cancellationToken)
    {
        if (!sessionPackageId.HasValue)
            return Result.Success();

        var packageStatus = await _sessionPackageService.GetPackageStatusAsync(
            sessionPackageId.Value, cancellationToken);

        if (packageStatus is null)
            return Result.Failure(new Error("SESSION_PACKAGE.NOT_FOUND", "Session package not found."));

        if (packageStatus.IsExpired)
            return Result.Failure(new Error("SESSION_PACKAGE.EXPIRED", "Session package has expired."));

        return Result.Success();
    }

    private static bool IsBookable(TrainerAvailability availability, BookSessionCommand request) =>
        !availability.IsDateBlocked(request.Date)
        && availability.HasAvailableSlot(request.Date.DayOfWeek, request.Time);
}
