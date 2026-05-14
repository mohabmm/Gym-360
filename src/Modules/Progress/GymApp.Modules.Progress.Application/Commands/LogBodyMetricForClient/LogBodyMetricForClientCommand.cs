using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;

public sealed record LogBodyMetricForClientCommand(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    DateOnly Date,
    decimal? Weight,
    string? WeightUnit,
    decimal? BodyFatPercentage,
    BodyMeasurementsDto? Measurements,
    string? Notes) : ICommand<Guid>;
