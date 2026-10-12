using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Commands.StartSession;

internal sealed class StartSessionCommandHandler : ICommandHandler<StartSessionCommand>
{
    // Default pre-window: 15 min. Trainers can start a session up to 15 minutes early.
    // Toggle the per-gym feature override "early_session_start" (testing mode) to bypass.
    private const int EarlyWindowMinutes = 15;
    private const string EarlyStartFeature = "early_session_start";

    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var allowEarlyStart = await _gymPlanService.IsFeatureEnabledAsync(
            session.GymId, EarlyStartFeature, cancellationToken);

        // The session's scheduled time is gym-local wall-clock, so compare it against
        // gym-local "now" — comparing with raw UtcNow kept Cairo trainers locked out
        // until 2–3 hours after the session was due to begin.
        var result = session.StartSession(
            GymLocalTime.FromUtc(_dateTimeProvider.UtcNow), EarlyWindowMinutes, allowEarlyStart);
        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
