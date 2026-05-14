using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

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
    private readonly IPublisher _publisher;

    public CancelSessionCommandHandler(
        ISessionRepository sessionRepository,
        IGymSessionPolicyService gymSessionPolicyService,
        IDateTimeProvider dateTimeProvider,
        ISchedulingUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _sessionRepository = sessionRepository;
        _gymSessionPolicyService = gymSessionPolicyService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isLateCancellation && session.Status == SessionStatus.LateCancelled)
        {
            await _publisher.Publish(
                new SessionLateCancelledIntegrationEvent(
                    session.Id,
                    session.TrainerId,
                    session.MemberId,
                    session.GymId,
                    request.CancelledBy),
                cancellationToken);
        }

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
