using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Scheduling.Application.Commands.CompleteSession;

internal sealed class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CompleteSessionCommandHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish the public integration event so cross-module subscribers (Billing
        // session-package decrement, Gamification, Communication) react. Outbox-driven
        // dispatch of domain events is handled separately, but in-process publish here
        // guarantees prompt fan-out without depending on the outbox poll cadence.
        await _publisher.Publish(
            new SessionCompletedIntegrationEvent(
                session.Id,
                session.TrainerId,
                session.MemberId,
                session.GymId),
            cancellationToken);

        return Result.Success();
    }
}
