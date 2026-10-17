using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateClass;

internal sealed class UpdateClassCommandHandler : ICommandHandler<UpdateClassCommand>
{
    private readonly IGymClassRepository _gymClassRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateClassCommandHandler(
        IGymClassRepository gymClassRepository,
        IGymPlanService gymPlanService,
        IGymsUnitOfWork unitOfWork)
    {
        _gymClassRepository = gymClassRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        // Feature gate: class_management (mirrors CreateClass).
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "class_management", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable("class_management", currentPlan, "Starter"));
        }

        var gymClass = await _gymClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (gymClass is null || gymClass.GymId != request.GymId)
            return Result.Failure(GymErrors.ClassNotFound);

        var schedule = new ClassSchedule(
            request.Schedule.DayOfWeek,
            request.Schedule.StartTime,
            request.Schedule.DurationMinutes,
            request.Schedule.IsRecurring);

        gymClass.Update(new GymClassDetails
        {
            Name = request.Name,
            Description = request.Description,
            Schedule = schedule,
            Capacity = request.Capacity,
            WaitlistEnabled = request.WaitlistEnabled
        });

        _gymClassRepository.Update(gymClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
