using System.Reflection;
using System.Text.Json;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Application.Exercises;

/// <summary>
/// The looping demo animation of a global library exercise, from free-exercise-db (public domain),
/// built by scripts/exercise-media/build_media.py and served by the API under
/// <see cref="BasePath"/>. Resolved when read, so every gym — old or new — has them without a
/// database change. A URL a gym set on the exercise itself always wins.
/// </summary>
internal static class ExerciseDemoMedia
{
    public const string BasePath = "/media/exercises/";

    private sealed record Entry(string File, string Source);

    private static readonly IReadOnlyDictionary<string, string> UrlsByName = Load();

    public static string? UrlFor(Exercise exercise) =>
        exercise.DemoMediaUrl ?? (exercise.Scope == ExerciseScope.Global ? UrlForName(exercise.Name) : null);

    public static string? UrlForName(string? name) =>
        name is not null && UrlsByName.TryGetValue(name, out var url) ? url : null;

    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("exercise-media-map.json")
            ?? throw new InvalidOperationException("exercise-media-map.json is not embedded.");
        var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return entries.ToDictionary(e => e.Key, e => BasePath + e.Value.File, StringComparer.OrdinalIgnoreCase);
    }
}
