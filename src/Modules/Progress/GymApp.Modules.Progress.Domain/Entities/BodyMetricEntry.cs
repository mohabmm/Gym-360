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
    public MetricSource Source { get; private set; }
    public Guid LoggedBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<BodyMetricEntry> Create(BodyMetricEntryDetails details)
    {
        var reading = details.Reading;

        if (reading.Weight is <= 0)
        {
            return Result.Failure<BodyMetricEntry>(ProgressErrors.InvalidWeight);
        }

        if (reading.BodyFatPercentage is < 0 or > 100)
        {
            return Result.Failure<BodyMetricEntry>(ProgressErrors.InvalidBodyFatPercentage);
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

    private void ApplyReading(BodyMetricReading reading)
    {
        Weight = reading.Weight;
        WeightUnit = reading.WeightUnit;
        BodyFatPercentage = reading.BodyFatPercentage;
        Measurements = reading.Measurements;
        Notes = reading.Notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
