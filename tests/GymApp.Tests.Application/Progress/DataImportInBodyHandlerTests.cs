using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Application.IntegrationEventHandlers;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymApp.Tests.Application.Progress;

/// <summary>Excel/Odoo InBody rows land in the real composition columns, not only in notes.</summary>
public sealed class DataImportInBodyHandlerTests
{
    [Fact]
    public async Task Imported_row_fills_muscle_and_water_and_keeps_the_rest_in_notes()
    {
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var repository = Substitute.For<IBodyMetricEntryRepository>();
        var lookup = Substitute.For<IGymMemberLookup>();
        lookup.GetUserIdByMemberNumberAsync(gymId, "7824", Arg.Any<CancellationToken>()).Returns(memberId);
        BodyMetricEntry? saved = null;
        await repository.AddAsync(Arg.Do<BodyMetricEntry>(e => saved = e), Arg.Any<CancellationToken>());

        var handler = new DataImportInBodyIntegrationEventHandler(
            repository, lookup, Substitute.For<IProgressUnitOfWork>(),
            NullLogger<DataImportInBodyIntegrationEventHandler>.Instance);

        await handler.Handle(new DataImportInBodyIntegrationEvent(
            Guid.NewGuid(), 2, gymId, "7824", new DateTime(2026, 10, 1),
            WeightKg: 80m, BodyFatPercentage: 18m, SkeletalMuscleMassKg: 35m, VisceralFatLevel: 7m,
            Bmi: 25m, BasalMetabolicRate: 1700m, BodyWaterKg: 45m, Notes: null), default);

        saved!.Source.Should().Be(MetricSource.InBodyScan);
        saved.Composition.Should().Be(new BodyComposition { SkeletalMuscleMassKg = 35m, BodyWaterKg = 45m });
        saved.Notes.Should().Be("Visceral: 7 | BMI: 25 | BMR: 1700");
    }
}
