using FluentValidation.TestHelper;
using GymApp.Modules.Training.Application.Commands.CreateWorkout;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Tests.Application.Validators;

public class CreateWorkoutCommandValidatorTests
{
    private readonly CreateWorkoutCommandValidator _validator = new();

    private static CreateWorkoutCommand CreateValidCommand() =>
        new(
            TrainerId: Guid.NewGuid(),
            GymId: Guid.NewGuid(),
            Name: "Chest Day",
            NameAr: null,
            Description: "Chest and triceps",
            Exercises: new List<WorkoutExerciseDto>
            {
                new(Guid.NewGuid(), 3, "10", 60m, null, 90, null, null)
            },
            EstimatedDuration: 45,
            IsTemplate: false);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTrainerId_HasValidationError()
    {
        var command = CreateValidCommand() with { TrainerId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TrainerId);
    }

    [Fact]
    public void Validate_EmptyGymId_HasValidationError()
    {
        var command = CreateValidCommand() with { GymId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.GymId);
    }

    [Fact]
    public void Validate_EmptyName_HasValidationError()
    {
        var command = CreateValidCommand() with { Name = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_HasValidationError()
    {
        var command = CreateValidCommand() with { Name = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyExercises_HasValidationError()
    {
        var command = CreateValidCommand() with { Exercises = new List<WorkoutExerciseDto>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Exercises);
    }

    [Fact]
    public void Validate_ZeroDuration_HasValidationError()
    {
        var command = CreateValidCommand() with { EstimatedDuration = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EstimatedDuration);
    }

    [Fact]
    public void Validate_NullDuration_PassesValidation()
    {
        var command = CreateValidCommand() with { EstimatedDuration = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.EstimatedDuration);
    }
}
