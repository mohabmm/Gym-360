using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetric;

public sealed record LogBodyMetricCommand(
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    decimal? Weight,
    string? WeightUnit,
    decimal? BodyFatPercentage,
    BodyMeasurementsDto? Measurements,
    string Source,
    Guid LoggedBy,
    string? Notes) : ICommand<Guid>;
