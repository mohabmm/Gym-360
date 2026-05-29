using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;

namespace GymApp.Modules.Scheduling.Application.Commands.CompleteSession;

internal sealed class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public CompleteSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SchedulingErrors.SessionNotFound(request.SessionId));

        var result = session.Complete();
        if (result.IsFailure)
            return result;

        _sessionRepository.Update(session);

        // Enqueue the public integration event so cross-module subscribers (Billing
        // session-package decrement + trainer earnings, Gamification, Communication) react.
        // Enqueued before SaveChanges so it commits to the outbox in this transaction and is
        // delivered durably (with retry) by the outbox processor — no longer lost if a
        // downstream handler throws after commit.
        _integrationEvents.Enqueue(
            new SessionCompletedIntegrationEvent(
                session.Id,
                session.TrainerId,
                session.MemberId,
                session.GymId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
