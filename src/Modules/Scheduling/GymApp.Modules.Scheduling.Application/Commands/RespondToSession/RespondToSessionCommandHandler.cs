using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Modules.Scheduling.Application.Commands.RespondToSession;

internal sealed class RespondToSessionCommandHandler : ICommandHandler<RespondToSessionCommand>
{
    private readonly ISessionRepository _sessions;
    private readonly IDateTimeProvider _clock;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public RespondToSessionCommandHandler(
        ISessionRepository sessions,
        IDateTimeProvider clock,
        IIntegrationEventPublisher integrationEvents,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _clock = clock;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RespondToSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var result = request.Confirmed
            ? session.ConfirmByMember(request.MemberId, _clock.UtcNow)
            : session.DisputeByMember(request.MemberId, request.Reason, _clock.UtcNow);
        if (result.IsFailure)
            return result;

        _sessions.Update(session);

        if (!request.Confirmed)
        {
            _integrationEvents.Enqueue(new SessionDisputedIntegrationEvent(
                session.Id, session.TrainerId, session.MemberId, session.GymId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
