using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Commands.CreateWorkout;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Tests.Application.Training;

public class CreateWorkoutCommandHandlerTests
{
    private readonly IWorkoutRepository _workoutRepository = Substitute.For<IWorkoutRepository>();
    private readonly ITrainingUnitOfWork _unitOfWork = Substitute.For<ITrainingUnitOfWork>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly CreateWorkoutCommandHandler _sut;

    public CreateWorkoutCommandHandlerTests()
    {
        _sut = new CreateWorkoutCommandHandler(_workoutRepository, _unitOfWork, _gymPlanService);
    }

    private static CreateWorkoutCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            TrainerId: Guid.NewGuid(),
            GymId: gymId ?? Guid.NewGuid(),
            Name: "Chest Day",
            NameAr: null,
            Description: "Chest and triceps workout",
            Exercises: new List<WorkoutExerciseDto>
            {
                new(Guid.NewGuid(), 3, "10", 60m, null, 90, null, null)
            },
            EstimatedDuration: 45,
            IsTemplate: false);

    [Fact]
    public async Task Handle_FeatureEnabled_CreatesWorkout()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "workout_builder", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _workoutRepository.Received(1).AddAsync(Arg.Any<Workout>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "workout_builder", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _workoutRepository.DidNotReceive().AddAsync(Arg.Any<Workout>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
