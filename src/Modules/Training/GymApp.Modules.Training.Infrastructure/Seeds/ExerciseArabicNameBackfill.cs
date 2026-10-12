using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Seeds;

/// <summary>
/// Gives the already-seeded global exercise library its Arabic names. Seed data only
/// reaches databases at creation time, and existing gym schemas were created while the
/// library was English-only, so this fills <c>NameAr</c> in place.
/// Idempotent: touches only global exercises whose Arabic name is still empty, and never
/// overwrites a name someone has since set.
/// </summary>
internal static class ExerciseArabicNameBackfill
{
    public static async Task<int> RunAsync(TrainingDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.Exercises
            .Where(e => e.Scope == ExerciseScope.Global && e.NameAr == null)
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var exercise in pending)
        {
            var arabic = ExerciseArabicNames.For(exercise.Name);
            if (arabic is null) continue;

            exercise.SetArabicName(arabic);
            updated++;
        }

        if (updated > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return updated;
    }
}
