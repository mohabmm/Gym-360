using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Analytics;

public sealed record GetPlatformAnalyticsQuery() : IQuery<PlatformAnalyticsDto>;

public sealed record GetChurnAnalyticsQuery() : IQuery<ChurnAnalyticsDto>;
