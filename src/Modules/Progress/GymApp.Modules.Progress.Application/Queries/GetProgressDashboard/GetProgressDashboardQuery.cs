using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressDashboard;

public sealed record GetProgressDashboardQuery(
    Guid UserId,
    Guid GymId) : IQuery<ProgressDashboardResponse>;
