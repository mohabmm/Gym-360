using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.MembershipPlans;

public sealed record GetMembershipPlansQuery(Guid GymId) : IQuery<List<MembershipPlanDto>>;
