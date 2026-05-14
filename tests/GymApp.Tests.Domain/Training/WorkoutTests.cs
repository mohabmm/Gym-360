using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Tests.Domain.Training;

public sealed class WorkoutTests
{
    private static Workout CreateDefaultWorkout(
        bool isTemplate = false,
        List<MuscleGroup>? muscleGroups = null)
    {
        return Workout.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Push Day",
            description: "Upper body push workout",
            estimatedDurationMinutes: 60,
            targetMuscleGroups: muscleGroups ?? new List<MuscleGroup> { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps },
            isTemplate: isTemplate);
    }

    [Fact]
    public void Create_ValidInput_ShouldSetPropertiesCorrectly()
    {
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();

        var workout = Workout.Create(trainerId, gymId, "Leg Day",
            description: "Legs", estimatedDurationMinutes: 45,
            targetMuscleGroups: new List<MuscleGroup> { MuscleGroup.Legs, MuscleGroup.Glutes });

        workout.Id.Should().NotBeEmpty();
        workout.TrainerId.Should().Be(trainerId);
        workout.GymId.Should().Be(gymId);
        workout.Name.Should().Be("Leg Day");
        workout.Description.Should().Be("Legs");
        workout.EstimatedDurationMinutes.Should().Be(45);
        workout.TargetMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Legs, MuscleGroup.Glutes });
        workout.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public void Create_AsTemplate_ShouldSetIsTemplateTrue()
    {
        var workout = CreateDefaultWorkout(isTemplate: true);

        workout.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseWorkoutCreatedDomainEvent()
    {
        var workout = CreateDefaultWorkout();

        workout.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<WorkoutCreatedDomainEvent>();
    }

    [Fact]
    public void Create_WithNullMuscleGroups_ShouldHaveEmptyList()
    {
        var workout = Workout.Create(Guid.NewGuid(), Guid.NewGuid(), "W");

        workout.TargetMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void AddExercise_ShouldAddWithCorrectOrder()
    {
        var workout = CreateDefaultWorkout();
        var exerciseId = Guid.NewGuid();

        var we = workout.AddExercise(exerciseId, 3, "12", weight: 50m, restSeconds: 90);

        workout.WorkoutExercises.Should().HaveCount(1);
        we.ExerciseId.Should().Be(exerciseId);
        we.Order.Should().Be(1);
        we.Sets.Should().Be(3);
        we.Reps.Should().Be("12");
        we.Weight.Should().Be(50m);
        we.RestSeconds.Should().Be(90);
    }

    [Fact]
    public void AddExercise_Multiple_ShouldAutoIncrementOrder()
    {
        var workout = CreateDefaultWorkout();

        workout.AddExercise(Guid.NewGuid(), 3, "10");
        workout.AddExercise(Guid.NewGuid(), 4, "8");
        workout.AddExercise(Guid.NewGuid(), 3, "12");

        workout.WorkoutExercises.Should().HaveCount(3);
        workout.WorkoutExercises[0].Order.Should().Be(1);
        workout.WorkoutExercises[1].Order.Should().Be(2);
        workout.WorkoutExercises[2].Order.Should().Be(3);
    }

    [Fact]
    public void RemoveExercise_ShouldRemoveAndReorder()
    {
        var workout = CreateDefaultWorkout();
        var we1 = workout.AddExercise(Guid.NewGuid(), 3, "10");
        var we2 = workout.AddExercise(Guid.NewGuid(), 4, "8");
        var we3 = workout.AddExercise(Guid.NewGuid(), 3, "12");

        workout.RemoveExercise(we2.Id);

        workout.WorkoutExercises.Should().HaveCount(2);
        workout.WorkoutExercises[0].Id.Should().Be(we1.Id);
        workout.WorkoutExercises[0].Order.Should().Be(1);
        workout.WorkoutExercises[1].Id.Should().Be(we3.Id);
        workout.WorkoutExercises[1].Order.Should().Be(2);
    }

    [Fact]
    public void RemoveExercise_NonExistentId_ShouldDoNothing()
    {
        var workout = CreateDefaultWorkout();
        workout.AddExercise(Guid.NewGuid(), 3, "10");

        workout.RemoveExercise(Guid.NewGuid());

        workout.WorkoutExercises.Should().HaveCount(1);
    }

    [Fact]
    public void Update_ShouldReplaceFieldsAndMuscleGroups()
    {
        var workout = CreateDefaultWorkout();

        workout.Update("Pull Day", null, "Back focus", 50,
            new List<MuscleGroup> { MuscleGroup.Back, MuscleGroup.Biceps });

        workout.Name.Should().Be("Pull Day");
        workout.Description.Should().Be("Back focus");
        workout.EstimatedDurationMinutes.Should().Be(50);
        workout.TargetMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Back, MuscleGroup.Biceps });
    }

    [Fact]
    public void Update_WithNullMuscleGroups_ShouldClearList()
    {
        var workout = CreateDefaultWorkout();

        workout.Update("New", null, null, null, null);

        workout.TargetMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void AddExercise_WithSuperset_ShouldSetGroupId()
    {
        var workout = CreateDefaultWorkout();
        var groupId = Guid.NewGuid();

        var we = workout.AddExercise(Guid.NewGuid(), 3, "10", supersetGroupId: groupId);

        we.SupersetGroupId.Should().Be(groupId);
    }
}
