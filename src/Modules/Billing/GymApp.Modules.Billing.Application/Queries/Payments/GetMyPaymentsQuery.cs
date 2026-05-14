using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

public sealed record GetMyPaymentsQuery(
    Guid UserId,
    Guid GymId,
    DateOnly? From,
    DateOnly? To,
    string Purpose,
    int Page,
    int PageSize) : IQuery<PagedResponse<MyPaymentDto>>;
