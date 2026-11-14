using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Progress;

/// <summary>InBody composition on a body-metric entry: validation, derived values, same-day merge.</summary>
public sealed class BodyCompositionTests
{
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Gym = Guid.NewGuid();

    private static BodyMetricReading Reading(
        decimal? weight = 80m, BodyComposition? composition = null, BodyMeasurements? measurements = null) =>
        new() { Weight = weight, WeightUnit = WeightUnit.Kg, Composition = composition, Measurements = measurements };

    private static BodyComposition Composition(decimal? height = 178m, decimal? muscle = 35m, decimal? water = 45m) =>
        new() { HeightCm = height, SkeletalMuscleMassKg = muscle, BodyWaterKg = water };

    private static Common.Domain.Result<BodyMetricEntry> Create(BodyMetricReading reading, MetricSource source = MetricSource.InBodyScan) =>
        BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = Member,
            GymId = Gym,
            Date = new DateOnly(2026, 11, 14),
            Source = source,
            LoggedBy = Member,
            Reading = reading,
        });

    [Fact]
    public void Entry_keeps_valid_composition()
    {
        var entry = Create(Reading(composition: Composition())).Value;

        entry.Composition.Should().Be(Composition());
    }

    [Fact]
    public void Empty_composition_is_stored_as_none()
    {
        var entry = Create(Reading(composition: new BodyComposition())).Value;

        entry.Composition.Should().BeNull();
    }

    [Theory]
    [InlineData(49.9)]
    [InlineData(250.1)]
    public void Height_outside_50_to_250_cm_is_rejected(decimal height)
    {
        var result = Create(Reading(composition: Composition(height: height)));

        result.Error.Should().Be(ProgressErrors.InvalidHeight);
    }

    [Theory]
    [InlineData(0, 45)]
    [InlineData(80, 45)]
    [InlineData(35, 81)]
    [InlineData(35, -1)]
    public void Muscle_or_water_must_be_positive_and_below_weight(decimal muscle, decimal water)
    {
        var result = Create(Reading(weight: 80m, composition: Composition(muscle: muscle, water: water)));

        result.Error.Should().Be(ProgressErrors.InvalidBodyComposition);
    }

    [Fact]
    public void Weight_in_pounds_is_converted_before_comparing()
    {
        // 176 lb ≈ 79.8 kg: 100 kg of water is below 176 but above the real weight, so it fails.
        var reading = new BodyMetricReading
        {
            Weight = 176m,
            WeightUnit = WeightUnit.Lbs,
            Composition = Composition(water: 100m),
        };

        Create(reading).Error.Should().Be(ProgressErrors.InvalidBodyComposition);
    }

    [Fact]
    public void Composition_without_weight_only_needs_positive_values()
    {
        Create(Reading(weight: null, composition: Composition())).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Percentages_and_bmi_are_derived_from_weight()
    {
        BodyComposition.PercentOf(35m, 80m).Should().Be(43.8m);
        BodyComposition.PercentOf(35m, null).Should().BeNull();
        BodyComposition.Bmi(80m, 178m).Should().Be(25.2m);
        BodyComposition.Bmi(80m, null).Should().BeNull();
    }

    [Fact]
    public void InBody_scan_on_a_logged_day_replaces_values_and_keeps_tape_measurements()
    {
        var tape = new BodyMeasurements { Waist = 90m, MeasurementUnit = MeasurementUnit.Cm };
        var entry = Create(Reading(weight: 82m, measurements: tape), MetricSource.SelfReported).Value;
        var scanner = Guid.NewGuid();

        var result = entry.RecordInBodyScan(Reading(weight: 80m, composition: Composition()), scanner);

        result.IsSuccess.Should().BeTrue();
        entry.Weight.Should().Be(80m);
        entry.Composition.Should().Be(Composition());
        entry.Measurements.Should().Be(tape);
        entry.Source.Should().Be(MetricSource.InBodyScan);
        entry.LoggedBy.Should().Be(scanner);
    }

    [Fact]
    public void Invalid_InBody_scan_leaves_the_entry_unchanged()
    {
        var entry = Create(Reading(weight: 82m), MetricSource.SelfReported).Value;

        var result = entry.RecordInBodyScan(Reading(weight: 80m, composition: Composition(height: 20m)), Member);

        result.Error.Should().Be(ProgressErrors.InvalidHeight);
        entry.Weight.Should().Be(82m);
        entry.Source.Should().Be(MetricSource.SelfReported);
    }
}
