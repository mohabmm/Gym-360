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

        // The endpoint is gated to the trainee role (RequireAuthorization("trainee")),
        // so a caller reaching this handler is always a trainee. A failed membership
        // check therefore means "this trainee has no relationship to this gym" —
        // NotAMemberOfGym, not MemberRoleRequired (which is for wrong-role callers).
        if (!await _membershipChecker.IsActiveMemberAsync(request.GymId, request.UserId, cancellationToken))
            return Result.Failure<PagedResponse<MyPaymentDto>>(GymAccessErrors.NotAMemberOfGym);

        var window = PageWindow.Create(request.Page, request.PageSize, MaxPageSize);

        var all = await _paymentRepository.GetByMemberAsync(
            request.GymId, request.UserId, cancellationToken);

        var ordered = ApplyFilters(all, request)
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .ToList();

        var items = window.Slice(ordered)
            .Select(ToDto)
            .ToList();

        return Result.Success(window.ToResponse(items, ordered.Count));
    }

    private static IEnumerable<Payment> ApplyFilters(IEnumerable<Payment> payments, GetMyPaymentsQuery request)
    {
        var filtered = ApplyDateRange(payments, request.From, request.To);

        return TryParsePurposeFilter(request.Purpose, out var purpose)
            ? filtered.Where(p => p.Purpose == purpose)
            : filtered;
    }

    private static IEnumerable<Payment> ApplyDateRange(IEnumerable<Payment> payments, DateOnly? from, DateOnly? to)
    {
        if (from is { } fromDate)
        {
            var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue);
            payments = payments.Where(p => p.PaidAt is not null && p.PaidAt.Value >= fromUtc);
        }

        if (to is { } toDate)
        {
            var toUtc = toDate.ToDateTime(TimeOnly.MaxValue);
            payments = payments.Where(p => p.PaidAt is not null && p.PaidAt.Value <= toUtc);
        }

        return payments;
    }

    // "All" (or blank) means no purpose filter; an unparseable purpose is ignored.
    private static bool TryParsePurposeFilter(string? purpose, out PaymentPurpose parsed)
    {
        parsed = default;
        return !string.IsNullOrWhiteSpace(purpose)
            && purpose != "All"
            && Enum.TryParse(purpose, ignoreCase: true, out parsed);
    }

    private static MyPaymentDto ToDto(Payment p) =>
        new(
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
            VoidedAt: null);
}
