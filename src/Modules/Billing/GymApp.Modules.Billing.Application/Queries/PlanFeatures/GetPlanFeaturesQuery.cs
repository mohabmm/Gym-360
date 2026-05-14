using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.PlanFeatures;

public sealed record GetPlanFeaturesQuery(Guid GymId) : IQuery<PlanFeaturesResponse>;
