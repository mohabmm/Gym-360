using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Commands.AssignWorkout;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Tests.Application.Training;

public class AssignWorkoutCommandHandlerTests
{
    private readonly IWorkoutAssignmentRepository _assignmentRepository = Substitute.For<IWorkoutAssignmentRepository>();
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly ITrainingUnitOfWork _unitOfWork = Substitute.For<ITrainingUnitOfWork>();
    private readonly AssignWorkoutCommandHandler _sut;

    public AssignWorkoutCommandHandlerTests()
    {
        _sut = new AssignWorkoutCommandHandler(_assignmentRepository, _workoutRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidWorkout_AssignsSuccessfully()
    {
        // Arrange
        var workoutId = Guid.NewGuid();
        var workout = Workout.Create(Guid.NewGuid(), Guid.NewGuid(), new WorkoutDetails { Name = "Test Workout" });
        _workoutRepository.GetByIdAsync(workoutId, Arg.Any<CancellationToken>()).Returns(workout);

        var command = new AssignWorkoutCommand(
            TrainerId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            GymId: Guid.NewGuid(),
            WorkoutId: workoutId,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(30),
            Notes: "Follow this plan",
            IsRemoteMode: false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _assignmentRepository.Received(1).AddAsync(Arg.Any<WorkoutAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WorkoutNotFound_ReturnsFailure()
    {
        // Arrange
        var workoutId = Guid.NewGuid();
        _workoutRepository.GetByIdAsync(workoutId, Arg.Any<CancellationToken>())
            .Returns((Workout?)null);

        var command = new AssignWorkoutCommand(
            TrainerId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            GymId: Guid.NewGuid(),
            WorkoutId: workoutId,
            StartDate: DateTime.UtcNow,
            EndDate: null,
            Notes: null,
            IsRemoteMode: false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(TrainingErrors.WorkoutNotFound.Code);
    }
}
