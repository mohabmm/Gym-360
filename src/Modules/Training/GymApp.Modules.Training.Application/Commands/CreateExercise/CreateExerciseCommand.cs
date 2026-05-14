using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.CreateExercise;

public sealed record CreateExerciseCommand(
    string Name,
    string? NameAr,
    string? Description,
    string? Instructions,
    string? DemoMediaUrl,
    List<string> MuscleGroups,
    string Equipment,
    string Category,
    string Scope,
    Guid? GymId,
    Guid? CreatedBy) : ICommand<Guid>;
