using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

public sealed record GetPaymentsQuery(
    Guid GymId,
    DateOnly? From,
    DateOnly? To,
    int Page,
    int PageSize) : IQuery<PagedResponse<PaymentDto>>;
