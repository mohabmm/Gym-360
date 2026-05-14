using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetPeakHoursAnalytics;

public sealed record GetPeakHoursAnalyticsQuery(
    Guid GymId,
    DateTime? From,
    DateTime? To,
    int TimezoneOffsetMinutes) : IQuery<PeakHoursAnalyticsResponse>;
