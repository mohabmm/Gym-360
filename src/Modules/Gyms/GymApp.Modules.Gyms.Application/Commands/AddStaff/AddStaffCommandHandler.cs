using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Services;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.AddStaff;

internal sealed class AddStaffCommandHandler : ICommandHandler<AddStaffCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IStaffRoleRepository _staffRoleRepository;
    private readonly GymSeatLimitChecker _seatLimits;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public AddStaffCommandHandler(
        IGymRepository gymRepository,
        IStaffRoleRepository staffRoleRepository,
        GymSeatLimitChecker seatLimits,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _staffRoleRepository = staffRoleRepository;
        _seatLimits = seatLimits;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(AddStaffCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var seat = await _seatLimits.EnsureStaffSeatAsync(request.GymId, cancellationToken);
        if (seat.IsFailure)
            return seat;

        // Check if user already has a staff role at this gym
        var existingRole = await _staffRoleRepository.GetByUserAndGymAsync(request.UserId, request.GymId, cancellationToken);
        if (existingRole is not null)
            return Result.Failure(new Error("Gyms.StaffAlreadyExists", "This user already has a staff role at this gym."));

        if (!Enum.TryParse<StaffRoleType>(request.Role, ignoreCase: true, out var roleType))
            return Result.Failure(new Error("Gyms.InvalidRole", "The specified staff role is invalid."));

        var staffRole = StaffRole.Create(request.GymId, request.UserId, roleType);

        await _staffRoleRepository.AddAsync(staffRole, cancellationToken);

        _integrationEvents.Enqueue(
            new StaffAddedToGymIntegrationEvent(request.GymId, request.UserId, roleType.ToString()));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
