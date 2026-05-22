using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gyms.Application.Commands.AddMember;

internal sealed class AddMemberCommandHandler : ICommandHandler<AddMemberCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymQueryService _gymQueryService;
    private readonly IGymMemberDirectory _memberDirectory;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public AddMemberCommandHandler(
        IGymRepository gymRepository,
        IGymPlanService gymPlanService,
        IGymQueryService gymQueryService,
        IGymMemberDirectory memberDirectory,
        IGymsUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _gymPlanService = gymPlanService;
        _gymQueryService = gymQueryService;
        _memberDirectory = memberDirectory;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var memberLimit = await _gymPlanService.GetMemberLimitAsync(request.GymId, cancellationToken);
        var currentCount = await _gymQueryService.GetMemberCountAsync(request.GymId, cancellationToken);

        if (memberLimit > 0 && currentCount >= memberLimit)
            return Result.Failure(PlanErrors.LimitExceeded("members", currentCount, memberLimit));

        var result = gym.AddMember(request.UserId);
        if (result.IsFailure)
            return result;

        _gymRepository.Update(gym);

        var assignedNumber = await _memberDirectory.RegisterAsync(
            request.GymId, request.UserId, request.MemberNumber, request.Section, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new MemberAddedToGymIntegrationEvent(
                request.GymId, request.UserId, request.Section, assignedNumber),
            cancellationToken);

        return Result.Success();
    }
}
