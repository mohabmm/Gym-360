using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

/// <summary>The gym's copy of the global exercise library by English name, so built-in plans link to it.</summary>
internal static class GlobalExerciseLookup
{
    public static async Task<IReadOnlyDictionary<string, (Guid Id, string? NameAr)>> LoadAsync(
        IExerciseRepository exercises, Guid gymId, CancellationToken cancellationToken) =>
        (await exercises.GetAllAsync(gymId, cancellationToken: cancellationToken))
            .Where(e => e.GymId is null)
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (g.First().Id, g.First().NameAr), StringComparer.OrdinalIgnoreCase);
}
