using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;

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
            new WorkoutDetails
            {
                Name = "Push Day",
                Description = "Upper body push workout",
                EstimatedDurationMinutes = 60,
                TargetMuscleGroups = muscleGroups ?? new List<MuscleGroup> { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }
            },
            isTemplate: isTemplate);
    }

    private static WorkoutExerciseDetails Prescription(Guid exerciseId, int sets, string reps) =>
        new() { ExerciseId = exerciseId, Sets = sets, Reps = reps };

    private static WorkoutExercise AddExercise(Workout workout, int sets, string reps) =>
        workout.AddExercise(Prescription(Guid.NewGuid(), sets, reps));

    [Fact]
    public void Create_ValidInput_ShouldSetPropertiesCorrectly()
    {
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();

        var workout = Workout.Create(trainerId, gymId, new WorkoutDetails
        {
            Name = "Leg Day",
            Description = "Legs",
            EstimatedDurationMinutes = 45,
            TargetMuscleGroups = new List<MuscleGroup> { MuscleGroup.Legs, MuscleGroup.Glutes }
        });

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
        var workout = Workout.Create(Guid.NewGuid(), Guid.NewGuid(), new WorkoutDetails { Name = "W" });

        workout.TargetMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void AddExercise_ShouldAddWithCorrectOrder()
    {
        var workout = CreateDefaultWorkout();
        var exerciseId = Guid.NewGuid();

        var we = workout.AddExercise(Prescription(exerciseId, 3, "12") with { Weight = 50m, RestSeconds = 90 });

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

        AddExercise(workout, 3, "10");
        AddExercise(workout, 4, "8");
        AddExercise(workout, 3, "12");

        workout.WorkoutExercises.Should().HaveCount(3);
        workout.WorkoutExercises[0].Order.Should().Be(1);
        workout.WorkoutExercises[1].Order.Should().Be(2);
        workout.WorkoutExercises[2].Order.Should().Be(3);
    }

    [Fact]
    public void RemoveExercise_ShouldRemoveAndReorder()
    {
        var workout = CreateDefaultWorkout();
        var we1 = AddExercise(workout, 3, "10");
        var we2 = AddExercise(workout, 4, "8");
        var we3 = AddExercise(workout, 3, "12");

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
        AddExercise(workout, 3, "10");

        workout.RemoveExercise(Guid.NewGuid());

        workout.WorkoutExercises.Should().HaveCount(1);
    }

    [Fact]
    public void Update_ShouldReplaceFieldsAndMuscleGroups()
    {
        var workout = CreateDefaultWorkout();

        workout.Update(new WorkoutDetails
        {
            Name = "Pull Day",
            Description = "Back focus",
            EstimatedDurationMinutes = 50,
            TargetMuscleGroups = new List<MuscleGroup> { MuscleGroup.Back, MuscleGroup.Biceps }
        });

        workout.Name.Should().Be("Pull Day");
        workout.Description.Should().Be("Back focus");
        workout.EstimatedDurationMinutes.Should().Be(50);
        workout.TargetMuscleGroups.Should().BeEquivalentTo(new[] { MuscleGroup.Back, MuscleGroup.Biceps });
    }

    [Fact]
    public void Update_WithNullMuscleGroups_ShouldClearList()
    {
        var workout = CreateDefaultWorkout();

        workout.Update(new WorkoutDetails { Name = "New" });

        workout.TargetMuscleGroups.Should().BeEmpty();
    }

    [Fact]
    public void AddExercise_WithSuperset_ShouldSetGroupId()
    {
        var workout = CreateDefaultWorkout();
        var groupId = Guid.NewGuid();

        var we = workout.AddExercise(Prescription(Guid.NewGuid(), 3, "10") with { SupersetGroupId = groupId });

        we.SupersetGroupId.Should().Be(groupId);
    }
}
