using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

internal sealed class QuickLogSessionCommandHandler : ICommandHandler<QuickLogSessionCommand, QuickLogSessionResult>
{
    /// <summary>A second tap within this window returns the session just logged instead of logging another.</summary>
    internal static readonly TimeSpan DoubleTapWindow = TimeSpan.FromMinutes(5);

    private const int DefaultDurationMinutes = 60;

    private readonly ISessionRepository _sessions;
    private readonly ISessionPackageService _packages;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IDateTimeProvider _clock;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public QuickLogSessionCommandHandler(
        ISessionRepository sessions,
        ISessionPackageService packages,
        IFeatureFlagService featureFlags,
        IDateTimeProvider clock,
        IIntegrationEventPublisher integrationEvents,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _packages = packages;
        _featureFlags = featureFlags;
        _clock = clock;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<QuickLogSessionResult>> Handle(QuickLogSessionCommand request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtDualSessionLogging, cancellationToken))
            return Result.Failure<QuickLogSessionResult>(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtDualSessionLogging));

        var nowUtc = _clock.UtcNow;
        var remaining = await _packages.GetRemainingWithTrainerAsync(
            request.MemberId, request.TrainerId, request.GymId, cancellationToken);

        var recent = await _sessions.FindRecentQuickLogAsync(
            request.TrainerId, request.MemberId, nowUtc - DoubleTapWindow, cancellationToken);
        if (recent is not null)
            return Result.Success(new QuickLogSessionResult(recent.Id, remaining ?? 0, AlreadyLogged: true));

        if (remaining is null)
            return Result.Failure<QuickLogSessionResult>(SchedulingErrors.ClientNotAssignedToTrainer);
        if (remaining <= 0)
            return Result.Failure<QuickLogSessionResult>(SchedulingErrors.NoSessionsRemaining);

        var nowLocal = GymLocalTime.FromUtc(nowUtc);
        var session = Session.LogCompleted(
            new SessionParticipants(request.GymId, request.TrainerId, request.MemberId),
            new SessionSchedule(DateOnly.FromDateTime(nowLocal), TimeOnly.FromDateTime(nowLocal), DefaultDurationMinutes),
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim());

        await _sessions.AddAsync(session, cancellationToken);

        // Same event as a normal completion: Billing deducts the package session (and fires
        // 3/1-left and trial alerts), Communication asks the trainee to confirm.
        _integrationEvents.Enqueue(new SessionCompletedIntegrationEvent(
            session.Id, session.TrainerId, session.MemberId, session.GymId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new QuickLogSessionResult(session.Id, remaining.Value - 1, AlreadyLogged: false));
    }
}
