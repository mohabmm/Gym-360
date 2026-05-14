using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Errors;

public static class TrainingErrors
{
    public static readonly Error ExerciseNotFound = new("Training.ExerciseNotFound", "The exercise was not found.");
    public static readonly Error WorkoutNotFound = new("Training.WorkoutNotFound", "The workout was not found.");
    public static readonly Error ProgramNotFound = new("Training.ProgramNotFound", "The program was not found.");
    public static readonly Error WorkoutLogNotFound = new("Training.WorkoutLogNotFound", "The workout log was not found.");
    public static readonly Error AssignmentNotFound = new("Training.AssignmentNotFound", "The assignment was not found.");
    public static readonly Error DuplicateExerciseName = new("Training.DuplicateExerciseName", "An exercise with this name already exists.");
    public static readonly Error InvalidProgramDuration = new("Training.InvalidProgramDuration", "Program duration must be between 1 and 52 weeks.");
    public static readonly Error RemoteWorkoutAlreadyActive = new("Training.RemoteWorkoutAlreadyActive", "Remote workout is already active for this client.");
    public static readonly Error RemoteWorkoutNotActive = new("Training.RemoteWorkoutNotActive", "Remote workout is not currently active for this client.");
    public static readonly Error TrainerNotAssignedToClient = new("Training.TrainerNotAssignedToClient", "The trainer is not assigned to this client.");
    public static readonly Error InvalidReps = new("Training.InvalidReps", "The reps value is invalid.");
}
