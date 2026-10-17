using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.Commands.CreateProgram;

internal sealed class CreateProgramCommandHandler : ICommandHandler<CreateProgramCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;

    public CreateProgramCommandHandler(
        IProgramRepository programRepository,
        ITrainingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService)
    {
        _programRepository = programRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<Guid>> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "multi_week_programs", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "multi_week_programs", currentPlan, "Pro"));
        }

        var program = Program.Create(
            request.TrainerId,
            request.GymId,
            new ProgramDetails
            {
                Name = request.Name,
                DurationWeeks = request.DurationWeeks,
                NameAr = request.NameAr,
                Description = request.Description
            },
            request.IsTemplate);

        foreach (var weekDto in request.Weeks)
        {
            var dayWorkouts = (weekDto.DayWorkouts ?? new List<ProgramDayWorkoutDto>())
                .Select(d => new ProgramDayWorkout(
                    Enum.Parse<DayOfWeek>(d.DayOfWeek, ignoreCase: true),
                    d.WorkoutId))
                .ToList();

            program.AddWeek(weekDto.WeekNumber, weekDto.Theme, dayWorkouts);
        }

        await _programRepository.AddAsync(program, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(program.Id);
    }
}
