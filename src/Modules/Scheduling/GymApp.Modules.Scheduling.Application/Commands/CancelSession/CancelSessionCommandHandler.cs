using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Modules.Scheduling.Application.Commands.CancelSession;

internal sealed class CancelSessionCommandHandler : ICommandHandler<CancelSessionCommand>
{
    // All gyms are in Egypt. ScheduledDate/ScheduledTime are stored as local
    // wall-clock time and must be converted to UTC before comparing with UtcNow.
    private static readonly TimeZoneInfo EgyptTimeZone = ResolveEgyptTimeZone();

    private readonly ISessionRepository _sessionRepository;
    private readonly IGymSessionPolicyService _gymSessionPolicyService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public CancelSessionCommandHandler(
        ISessionRepository sessionRepository,
        IGymSessionPolicyService gymSessionPolicyService,
        IDateTimeProvider dateTimeProvider,
        ISchedulingUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _sessionRepository = sessionRepository;
        _gymSessionPolicyService = gymSessionPolicyService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(CancelSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var policy = await _gymSessionPolicyService.GetPolicyAsync(session.GymId, cancellationToken);

        var cancellationWindowHours = policy?.CancellationWindowHours ?? 24;

        var localSessionDateTime = session.ScheduledDate.ToDateTime(session.ScheduledTime);
        var sessionUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localSessionDateTime, DateTimeKind.Unspecified),
            EgyptTimeZone);

        var hoursUntilSession = (sessionUtc - _dateTimeProvider.UtcNow).TotalHours;
        var isLateCancellation = hoursUntilSession < cancellationWindowHours;

        var reason = request.Reason ?? "No reason provided";

        var result = session.Cancel(reason, request.CancelledBy, isLateCancellation);
        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);

        // Status is set by session.Cancel above, so the late-cancel decision is known before
        // commit. Enqueue before SaveChanges so the event commits to the outbox in this
        // transaction and is delivered durably (with retry) by the outbox processor.
        if (isLateCancellation && session.Status == SessionStatus.LateCancelled)
        {
            _integrationEvents.Enqueue(
                new SessionLateCancelledIntegrationEvent(
                    session.Id,
                    session.TrainerId,
                    session.MemberId,
                    session.GymId,
                    request.CancelledBy));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static TimeZoneInfo ResolveEgyptTimeZone()
    {
        // .NET 8 accepts both IANA and Windows IDs via the TZ data, but fall back
        // just in case the host OS only ships one family.
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        throw new InvalidOperationException("Egypt time zone is not available on this host.");
    }
}
