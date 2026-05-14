using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

internal sealed class GetPaymentsQueryHandler : IQueryHandler<GetPaymentsQuery, PagedResponse<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PagedResponse<PaymentDto>>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var to = request.To?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        var payments = await _paymentRepository.GetByGymIdAsync(
            request.GymId, from, to, request.Page, request.PageSize, cancellationToken);

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
            p.CreatedAt)).ToList();

        // Total count would ideally come from repository; using payments.Count as approximation
        var totalCount = payments.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var response = new PagedResponse<PaymentDto>(
            dtos, request.Page, request.PageSize, totalCount, totalPages);

        return Result.Success(response);
    }
}
