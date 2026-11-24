namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>Every ready program, in the order the coach sees them.</summary>
internal static class ProgramCatalog
{
    public static IReadOnlyList<ReadyProgram> All { get; } = ClassicPrograms.All
        .Concat(EgyptianPrograms.All)
        .Concat(LifestylePrograms.All)
        .Concat(CardioPrograms.All)
        .ToList();

    public static ReadyProgram? Find(string key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
