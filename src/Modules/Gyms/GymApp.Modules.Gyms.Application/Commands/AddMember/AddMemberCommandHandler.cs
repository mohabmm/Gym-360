using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Services;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.AddMember;

internal sealed class AddMemberCommandHandler : ICommandHandler<AddMemberCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly GymSeatLimitChecker _seatLimits;
    private readonly IGymMemberDirectory _memberDirectory;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public AddMemberCommandHandler(
        IGymRepository gymRepository,
        GymSeatLimitChecker seatLimits,
        IGymMemberDirectory memberDirectory,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _seatLimits = seatLimits;
        _memberDirectory = memberDirectory;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var seat = await _seatLimits.EnsureMemberSeatAsync(request.GymId, cancellationToken);
        if (seat.IsFailure)
            return seat;

        var result = gym.AddMember(request.UserId);
        if (result.IsFailure)
            return result;

        _gymRepository.Update(gym);

        var assignedNumber = await _memberDirectory.RegisterAsync(
            new GymMemberRegistration(request.GymId, request.UserId, request.MemberNumber, request.Section),
            cancellationToken);

        _integrationEvents.Enqueue(
            new MemberAddedToGymIntegrationEvent(
                request.GymId, request.UserId, request.Section, assignedNumber));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
