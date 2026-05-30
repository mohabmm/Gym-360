using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

internal sealed class GetPaymentsQueryHandler : IQueryHandler<GetPaymentsQuery, PagedResponse<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public GetPaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        IUserDisplayLookup userDisplayLookup)
    {
        _paymentRepository = paymentRepository;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<Result<PagedResponse<PaymentDto>>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var to = request.To?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        var payments = await _paymentRepository.GetByGymIdAsync(
            request.GymId, from, to, request.Page, request.PageSize, cancellationToken);

        // payment.MemberId is the trainee's Identity user id (see RecordPaymentCommand /
        // GymMembershipChecker), so the cross-tenant Identity directory resolves names.
        var names = await _userDisplayLookup.GetDisplayNamesAsync(
            payments.Select(p => p.MemberId), cancellationToken);

        var dtos = payments.Select(p => new PaymentDto(
            p.Id,
            p.GymId,
            p.MemberId,
            p.Amount.Amount,
            p.Amount.Currency,
            p.PaymentMethod.ToString(),
            p.Purpose.ToString(),
            p.ReferenceId,
            p.Status.ToString(),
            p.ReceiptNumber,
            p.RecordedBy,
            p.VoidReason,
            p.PaidAt,
            p.CreatedAt,
            names.TryGetValue(p.MemberId, out var d) ? d.FullName : null)).ToList();

        // Total count would ideally come from repository; using payments.Count as approximation
        var totalCount = payments.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var response = new PagedResponse<PaymentDto>(
            dtos, request.Page, request.PageSize, totalCount, totalPages);

        return Result.Success(response);
    }
}
