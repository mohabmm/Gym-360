using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.ValueObjects;

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

        var assignment = WorkoutAssignment.Create(new WorkoutAssignmentDetails
        {
            TrainerId = request.TrainerId,
            ClientId = request.ClientId,
            GymId = request.GymId,
            AssignmentType = AssignmentType.Workout,
            StartDate = DateOnly.FromDateTime(request.StartDate),
            WorkoutId = request.WorkoutId,
            EndDate = request.EndDate.HasValue ? DateOnly.FromDateTime(request.EndDate.Value) : null,
            Notes = request.Notes,
            IsRemoteMode = request.IsRemoteMode
        });

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(assignment.Id);
    }
}
