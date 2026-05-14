using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymAnalytics;

public sealed record GetGymAnalyticsQuery(Guid GymId) : IQuery<GymAnalyticsResponse>;
