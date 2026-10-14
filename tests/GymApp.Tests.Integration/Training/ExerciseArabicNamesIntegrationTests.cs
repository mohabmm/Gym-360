using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Infrastructure;
using GymApp.Modules.Training.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// The global exercise library must carry Arabic names (Arabic-first product): via seed
/// data for fresh databases, and via the startup backfill for databases created while the
/// library was English-only.
/// </summary>
public sealed class ExerciseArabicNamesIntegrationTests : BaseIntegrationTest
{
    public ExerciseArabicNamesIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task SeededLibrary_EveryGlobalExerciseHasAnArabicName()
    {
        var db = GetService<TrainingDbContext>();

        var global = await db.Exercises.AsNoTracking()
            .Where(e => e.Scope == ExerciseScope.Global)
            .Select(e => new { e.Name, e.NameAr })
            .ToListAsync();

        global.Should().HaveCountGreaterThan(200);
        global.Where(e => string.IsNullOrWhiteSpace(e.NameAr)).Select(e => e.Name)
            .Should().BeEmpty("every library exercise needs an Arabic name");
        global.Should().OnlyContain(e => e.NameAr!.Any(c => c >= '؀' && c <= 'ۿ'));
    }

    [Fact]
    public async Task Backfill_RestoresMissingNames_AndKeepsExistingOnes()
    {
        Guid clearedId, customId;
        string expectedArabic;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();
            var pair = await db.Exercises
                .Where(e => e.Scope == ExerciseScope.Global && e.NameAr != null)
                .OrderBy(e => e.Name).Take(2).ToListAsync();
            expectedArabic = pair[0].NameAr!;
            pair[0].SetArabicName(null);          // simulates a pre-Arabic database row
            pair[1].SetArabicName("اسم معدل");     // a name someone has since changed
            clearedId = pair[0].Id;
            customId = pair[1].Id;
            await db.SaveChangesAsync();
        }

        int updated;
        using (var scope = Factory.Services.CreateScope())
            updated = await TrainingModuleRegistration.BackfillExerciseArabicNamesAsync(scope.ServiceProvider);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();
            updated.Should().BeGreaterOrEqualTo(1);
            (await db.Exercises.AsNoTracking().SingleAsync(e => e.Id == clearedId)).NameAr
                .Should().Be(expectedArabic);
            (await db.Exercises.AsNoTracking().SingleAsync(e => e.Id == customId)).NameAr
                .Should().Be("اسم معدل");

            // Second run is a no-op.
            (await TrainingModuleRegistration.BackfillExerciseArabicNamesAsync(scope.ServiceProvider))
                .Should().Be(0);
        }
    }
}
