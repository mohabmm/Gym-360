namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record BodyMetricDto(
    Guid Id,
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    decimal? Weight,
    string WeightUnit,
    decimal? BodyFatPercentage,
    BodyMeasurementsDto? Measurements,
    string Source,
    Guid LoggedBy,
    string? Notes,
    DateTime CreatedAt);
