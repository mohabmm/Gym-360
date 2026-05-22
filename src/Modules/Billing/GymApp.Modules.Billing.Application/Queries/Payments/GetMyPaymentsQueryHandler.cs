using GymApp.Modules.Gyms.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Payments;

internal sealed class GetMyPaymentsQueryHandler
    : IQueryHandler<GetMyPaymentsQuery, PagedResponse<MyPaymentDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 50;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IGymMembershipChecker _membershipChecker;

    public GetMyPaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        IGymMembershipChecker membershipChecker)
    {
        _paymentRepository = paymentRepository;
        _membershipChecker = membershipChecker;
    }

    public async Task<Result<PagedResponse<MyPaymentDto>>> Handle(
        GetMyPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _membershipChecker.IsGymSuspendedAsync(request.GymId, cancellationToken))
            return Result.Failure<PagedResponse<MyPaymentDto>>(GymAccessErrors.GymSuspended);

        if (!await _membershipChecker.IsActiveMemberAsync(request.GymId, request.UserId, cancellationToken))
            return Result.Failure<PagedResponse<MyPaymentDto>>(GymAccessErrors.MemberRoleRequired);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, MinPageSize, MaxPageSize);

        var all = await _paymentRepository.GetByMemberAsync(
            request.GymId, request.UserId, cancellationToken);

        IEnumerable<Payment> filtered = all;

        if (request.From is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue);
            filtered = filtered.Where(p => p.PaidAt is not null && p.PaidAt.Value >= fromUtc);
        }

        if (request.To is { } to)
        {
            var toUtc = to.ToDateTime(TimeOnly.MaxValue);
            filtered = filtered.Where(p => p.PaidAt is not null && p.PaidAt.Value <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Purpose) && request.Purpose != "All")
        {
            if (Enum.TryParse<PaymentPurpose>(request.Purpose, ignoreCase: true, out var purposeEnum))
                filtered = filtered.Where(p => p.Purpose == purposeEnum);
        }

        var ordered = filtered
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new MyPaymentDto(
                p.Id,
                p.ReceiptNumber,
                new MoneyDto(p.Amount.Amount, p.Amount.Currency),
                p.PaymentMethod.ToString(),
                p.Purpose.ToString(),
                p.ReferenceId,
                // Reference label + recorded-by staff name are cross-module
                // lookups (Billing would need read models that don't yet
                // exist). Returning null preserves module boundaries and
                // matches the PRD's nullable contract for these fields.
                ReferenceLabel: null,
                p.Status.ToString(),
                p.PaidAt,
                RecordedByStaffName: null,
                p.VoidReason,
                // Payment aggregate does not track VoidedAt separately.
                // When the domain gains a VoidedAt timestamp this can be wired.
                VoidedAt: null))
            .ToList();

        var response = new PagedResponse<MyPaymentDto>(items, page, pageSize, totalCount, totalPages);
        return Result.Success(response);
    }
}
