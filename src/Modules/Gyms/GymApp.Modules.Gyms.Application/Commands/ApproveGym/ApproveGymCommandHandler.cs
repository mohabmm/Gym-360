using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveGym;

internal sealed class ApproveGymCommandHandler : ICommandHandler<ApproveGymCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ApproveGymCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cross-module fan-out: Billing handler attaches default Pro subscription so
        // plan-gated features work post-approval. Same event fired by RegisterGym
        // when AutoApprove=true.
        await _publisher.Publish(new GymApprovedIntegrationEvent(gym.Id, gym.OwnerId), cancellationToken);

        return Result.Success();
    }
}
