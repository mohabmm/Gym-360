using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetBodyMetricTrends;

public sealed record GetBodyMetricTrendsQuery(
    Guid UserId,
    Guid GymId,
    DateOnly From,
    DateOnly To) : IQuery<MetricTrendsResponse>;
