using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Domain.Entities;

public sealed class BodyMetricEntry : Entity
{
    private BodyMetricEntry() { }

    private BodyMetricEntry(
        Guid id,
        Guid userId,
        Guid gymId,
        DateOnly date,
        decimal? weight,
        WeightUnit weightUnit,
        decimal? bodyFatPercentage,
        BodyMeasurements? measurements,
        MetricSource source,
        Guid loggedBy,
        string? notes)
        : base(id)
    {
        UserId = userId;
        GymId = gymId;
        Date = date;
        Weight = weight;
        WeightUnit = weightUnit;
        BodyFatPercentage = bodyFatPercentage;
        Measurements = measurements;
        Source = source;
        LoggedBy = loggedBy;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

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

    public static Result<BodyMetricEntry> Create(
        Guid userId,
        Guid gymId,
        DateOnly date,
        decimal? weight,
        WeightUnit weightUnit,
        decimal? bodyFatPercentage,
        BodyMeasurements? measurements,
        MetricSource source,
        Guid loggedBy,
        string? notes = null)
    {
        if (weight.HasValue && weight.Value <= 0)
        {
            return Result.Failure<BodyMetricEntry>(ProgressErrors.InvalidWeight);
        }

        if (bodyFatPercentage.HasValue && (bodyFatPercentage.Value < 0 || bodyFatPercentage.Value > 100))
        {
            return Result.Failure<BodyMetricEntry>(ProgressErrors.InvalidBodyFatPercentage);
        }

        var entry = new BodyMetricEntry(
            Guid.NewGuid(),
            userId,
            gymId,
            date,
            weight,
            weightUnit,
            bodyFatPercentage,
            measurements,
            source,
            loggedBy,
            notes);

        entry.RaiseDomainEvent(new BodyMetricLoggedDomainEvent(entry.Id, userId));

        return Result.Success(entry);
    }

    public void Update(
        decimal? weight,
        WeightUnit weightUnit,
        decimal? bodyFatPercentage,
        BodyMeasurements? measurements,
        string? notes)
    {
        Weight = weight;
        WeightUnit = weightUnit;
        BodyFatPercentage = bodyFatPercentage;
        Measurements = measurements;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
