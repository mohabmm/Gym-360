using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Modules.Scheduling.Application.Commands.ReverseSession;

internal sealed class ReverseSessionCommandHandler : ICommandHandler<ReverseSessionCommand>
{
    private readonly ISessionRepository _sessions;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public ReverseSessionCommandHandler(
        ISessionRepository sessions,
        IIntegrationEventPublisher integrationEvents,
        ISchedulingUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReverseSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.GymId != request.GymId)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var result = session.ReverseDisputed();
        if (result.IsFailure)
            return result;

        _sessions.Update(session);
        _integrationEvents.Enqueue(new SessionReversedIntegrationEvent(
            session.Id, session.TrainerId, session.MemberId, session.GymId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
