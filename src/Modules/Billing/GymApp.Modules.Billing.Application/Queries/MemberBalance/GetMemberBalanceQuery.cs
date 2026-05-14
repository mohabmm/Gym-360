using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.MemberBalance;

public sealed record GetMemberBalanceQuery(
    Guid GymId,
    Guid MemberId) : IQuery<MemberBalanceResponse>;
