using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Plans;

public sealed record GetAllPlansQuery() : IQuery<List<SubscriptionPlanDto>>;
