using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.GymSubscriptions;

public sealed record GetGymSubscriptionQuery(Guid GymId) : IQuery<GymSubscriptionDetailDto>;
