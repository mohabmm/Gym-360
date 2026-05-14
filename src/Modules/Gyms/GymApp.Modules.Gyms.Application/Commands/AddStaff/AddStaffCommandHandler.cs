using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gyms.Application.Commands.AddStaff;

internal sealed class AddStaffCommandHandler : ICommandHandler<AddStaffCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IStaffRoleRepository _staffRoleRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymQueryService _gymQueryService;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public AddStaffCommandHandler(
        IGymRepository gymRepository,
        IStaffRoleRepository staffRoleRepository,
        IGymPlanService gymPlanService,
        IGymQueryService gymQueryService,
        IGymsUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _staffRoleRepository = staffRoleRepository;
        _gymPlanService = gymPlanService;
        _gymQueryService = gymQueryService;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(AddStaffCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var staffLimit = await _gymPlanService.GetStaffLimitAsync(request.GymId, cancellationToken);
        var currentCount = await _gymQueryService.GetStaffCountAsync(request.GymId, cancellationToken);

        if (staffLimit > 0 && currentCount >= staffLimit)
            return Result.Failure(PlanErrors.LimitExceeded("staff", currentCount, staffLimit));

        // Check if user already has a staff role at this gym
        var existingRole = await _staffRoleRepository.GetByUserAndGymAsync(request.UserId, request.GymId, cancellationToken);
        if (existingRole is not null)
            return Result.Failure(new Error("Gyms.StaffAlreadyExists", "This user already has a staff role at this gym."));

        if (!Enum.TryParse<StaffRoleType>(request.Role, ignoreCase: true, out var roleType))
            return Result.Failure(new Error("Gyms.InvalidRole", "The specified staff role is invalid."));

        var staffRole = StaffRole.Create(request.GymId, request.UserId, roleType);

        await _staffRoleRepository.AddAsync(staffRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new StaffAddedToGymIntegrationEvent(request.GymId, request.UserId, roleType.ToString()),
            cancellationToken);

        return Result.Success();
    }
}
