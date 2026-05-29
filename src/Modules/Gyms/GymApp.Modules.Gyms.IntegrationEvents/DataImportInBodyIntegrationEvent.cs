using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record DataImportInBodyIntegrationEvent(
    Guid ImportJobId,
    int RowNumber,
    Guid GymId,
    string MemberNumber,
    DateTime MeasuredAt,
    decimal? WeightKg,
    decimal? BodyFatPercentage,
    decimal? SkeletalMuscleMassKg,
    decimal? VisceralFatLevel,
    decimal? Bmi,
    decimal? BasalMetabolicRate,
    decimal? BodyWaterKg,
    string? Notes) : IIntegrationEvent;
