using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

internal sealed class GetMemberPaymentsQueryHandler
    : IQueryHandler<GetMemberPaymentsQuery, List<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public GetMemberPaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        IUserDisplayLookup userDisplayLookup)
    {
        _paymentRepository = paymentRepository;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<Result<List<PaymentDto>>> Handle(
        GetMemberPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

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

        return Result.Success(dtos);
    }
}
