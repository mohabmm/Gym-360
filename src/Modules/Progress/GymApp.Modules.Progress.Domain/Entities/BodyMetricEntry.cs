using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Domain.Entities;

public sealed class BodyMetricEntry : Entity
{
    private BodyMetricEntry() { }

    private BodyMetricEntry(Guid id) : base(id) { }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal? Weight { get; private set; }
    public WeightUnit WeightUnit { get; private set; }
    public decimal? BodyFatPercentage { get; private set; }
    public BodyMeasurements? Measurements { get; private set; }
    public BodyComposition? Composition { get; private set; }
    public MetricSource Source { get; private set; }
    public Guid LoggedBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<BodyMetricEntry> Create(BodyMetricEntryDetails details)
    {
        var reading = details.Reading;

        if (Validate(reading) is { } error)
        {
            return Result.Failure<BodyMetricEntry>(error);
        }

        var entry = new BodyMetricEntry(Guid.NewGuid())
        {
            UserId = details.UserId,
            GymId = details.GymId,
            Date = details.Date,
            Source = details.Source,
            LoggedBy = details.LoggedBy,
            CreatedAt = DateTime.UtcNow
        };
        entry.ApplyReading(reading);

        entry.RaiseDomainEvent(new BodyMetricLoggedDomainEvent(entry.Id, details.UserId));

        return Result.Success(entry);
    }

    public void Update(BodyMetricReading reading)
    {
        ApplyReading(reading);
    }

    /// <summary>
    /// Applies an InBody scan to an entry that already exists for the same day. The scan's weight,
    /// body fat and composition replace the day's values; tape measurements already logged are kept
    /// when the scan has none. The entry becomes an InBody entry logged by <paramref name="loggedBy"/>.
    /// </summary>
    public Result RecordInBodyScan(BodyMetricReading scan, Guid loggedBy)
    {
        if (Validate(scan) is { } error)
        {
            return Result.Failure(error);
        }

        ApplyReading(scan with
        {
            Measurements = scan.Measurements ?? Measurements,
            Notes = scan.Notes ?? Notes
        });
        Source = MetricSource.InBodyScan;
        LoggedBy = loggedBy;

        return Result.Success();
    }

    private static Error? Validate(BodyMetricReading reading)
    {
        if (reading.Weight is <= 0)
            return ProgressErrors.InvalidWeight;

        if (reading.BodyFatPercentage is < 0 or > 100)
            return ProgressErrors.InvalidBodyFatPercentage;

        return reading.Composition?.Validate(reading.WeightKg);
    }

    private void ApplyReading(BodyMetricReading reading)
    {
        Weight = reading.Weight;
        WeightUnit = reading.WeightUnit;
        BodyFatPercentage = reading.BodyFatPercentage;
        Measurements = reading.Measurements;
        Composition = reading.Composition is { IsEmpty: false } composition ? composition : null;
        Notes = reading.Notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
