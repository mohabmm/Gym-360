using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveGym;

internal sealed class ApproveGymCommandHandler : ICommandHandler<ApproveGymCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public ApproveGymCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(ApproveGymCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var result = gym.Approve();
        if (result.IsFailure)
            return result;

        _gymRepository.Update(gym);

        // Cross-module fan-out: Billing handler attaches default Pro subscription so
        // plan-gated features work post-approval. Same event fired by RegisterGym
        // when AutoApprove=true. Enqueued before SaveChanges so it commits to the outbox
        // in this transaction and is delivered (with retry) by the outbox processor.
        _integrationEvents.Enqueue(new GymApprovedIntegrationEvent(gym.Id, gym.OwnerId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
