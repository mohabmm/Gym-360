using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.CreateExercise;

internal sealed class CreateExerciseCommandHandler : ICommandHandler<CreateExerciseCommand, Guid>
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;

    public CreateExerciseCommandHandler(
        IExerciseRepository exerciseRepository,
        ITrainingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService)
    {
        _exerciseRepository = exerciseRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<Guid>> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var scope = Enum.Parse<ExerciseScope>(request.Scope, ignoreCase: true);

        // Feature gating only for GymSpecific exercises
        if (scope == ExerciseScope.GymSpecific && request.GymId.HasValue)
        {
            var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
                request.GymId.Value, "custom_exercise_library", cancellationToken);

            if (!isEnabled)
            {
                var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId.Value, cancellationToken);
                return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                    "custom_exercise_library", currentPlan, "Business"));
            }
        }

        var equipment = Enum.Parse<Equipment>(request.Equipment, ignoreCase: true);
        var category = Enum.Parse<ExerciseCategory>(request.Category, ignoreCase: true);
        var muscleGroups = request.MuscleGroups
            .Select(mg => Enum.Parse<MuscleGroup>(mg, ignoreCase: true))
            .ToList();

        var exercise = Exercise.Create(
            request.Name,
            equipment,
            category,
            scope,
            muscleGroups,
            request.NameAr,
            request.Description,
            request.Instructions,
            request.DemoMediaUrl,
            request.GymId,
            request.CreatedBy);

        await _exerciseRepository.AddAsync(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(exercise.Id);
    }
}
