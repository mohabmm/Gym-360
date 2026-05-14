using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetBodyMetrics;

public sealed record GetBodyMetricsQuery(
    Guid UserId,
    Guid GymId,
    DateOnly? From,
    DateOnly? To) : IQuery<List<BodyMetricDto>>;
