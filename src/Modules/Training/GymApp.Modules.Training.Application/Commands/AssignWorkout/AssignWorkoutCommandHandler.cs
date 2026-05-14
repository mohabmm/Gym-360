using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.AssignWorkout;

internal sealed class AssignWorkoutCommandHandler : ICommandHandler<AssignWorkoutCommand, Guid>
{
    private readonly IWorkoutAssignmentRepository _assignmentRepository;
    private readonly IWorkoutRepository _workoutRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public AssignWorkoutCommandHandler(
        IWorkoutAssignmentRepository assignmentRepository,
        IWorkoutRepository workoutRepository,
        ITrainingUnitOfWork unitOfWork)
    {
        _assignmentRepository = assignmentRepository;
        _workoutRepository = workoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AssignWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = await _workoutRepository.GetByIdAsync(request.WorkoutId, cancellationToken);

        if (workout is null)
        {
            return Result.Failure<Guid>(TrainingErrors.WorkoutNotFound);
        }

        var assignment = WorkoutAssignment.Create(
            request.TrainerId,
            request.ClientId,
            request.GymId,
            AssignmentType.Workout,
            DateOnly.FromDateTime(request.StartDate),
            workoutId: request.WorkoutId,
            endDate: request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null,
            notes: request.Notes,
            isRemoteMode: request.IsRemoteMode);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(assignment.Id);
    }
}
