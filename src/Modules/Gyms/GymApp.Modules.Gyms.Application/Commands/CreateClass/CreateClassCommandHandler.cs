using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.CreateClass;

internal sealed class CreateClassCommandHandler : ICommandHandler<CreateClassCommand, Guid>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymClassRepository _gymClassRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymsUnitOfWork _unitOfWork;

    public CreateClassCommandHandler(
        IGymRepository gymRepository,
        IGymClassRepository gymClassRepository,
        IGymPlanService gymPlanService,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _gymClassRepository = gymClassRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        // Feature gate: class_management
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "class_management", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("class_management", currentPlan, "Starter"));
        }

        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<Guid>(GymErrors.NotFound);

        var schedule = new ClassSchedule(
            request.Schedule.DayOfWeek,
            request.Schedule.StartTime,
            request.Schedule.DurationMinutes,
            request.Schedule.IsRecurring);

        var gymClass = GymClass.Create(
            request.GymId,
            request.Name,
            request.Description,
            request.TrainerId,
            schedule,
            request.Capacity,
            request.WaitlistEnabled);

        await _gymClassRepository.AddAsync(gymClass, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(gymClass.Id);
    }
}
