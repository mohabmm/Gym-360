using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

// Returns ALL payments for a single member (no pagination). The previous
// approach fetched the gym's first 200 payments and filtered client-side,
// which dropped a member's payments once a gym had more than 200 in total.
public sealed record GetMemberPaymentsQuery(
    Guid GymId,
    Guid MemberId) : IQuery<List<PaymentDto>>;
