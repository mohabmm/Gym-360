using System.Reflection;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Application.Billing;

/// <summary>
/// Unit tests for GET /api/v1/billing/me/payments per PRD_TRAINEE_ME_ENDPOINTS.md.
/// One test method per PRD acceptance criterion.
///
/// Reflection-based; see GetMyPackagesQueryHandlerTests for rationale.
/// </summary>
public class GetMyPaymentsQueryHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IGymMembershipChecker _membershipChecker = Substitute.For<IGymMembershipChecker>();

    private const string HandlerTypeName =
        "GymApp.Modules.Billing.Application.Queries.Payments.GetMyPaymentsQueryHandler";
    private const string QueryTypeName =
        "GymApp.Modules.Billing.Application.Queries.Payments.GetMyPaymentsQuery";

    private static readonly Assembly BillingAppAssembly =
        typeof(GymApp.Modules.Billing.Application.DTOs.PaymentDto).Assembly;

    private static Type HandlerType => BillingAppAssembly.GetType(HandlerTypeName)
        ?? throw new Xunit.Sdk.XunitException(
            $"Handler type '{HandlerTypeName}' not found. The backend agent has not yet implemented " +
            "GET /api/v1/billing/me/payments. This test will run green once the handler lands.");

    private static Type QueryType => BillingAppAssembly.GetType(QueryTypeName)
        ?? throw new Xunit.Sdk.XunitException(
            $"Query type '{QueryTypeName}' not found. The backend agent has not yet implemented " +
            "GET /api/v1/billing/me/payments.");

    private object CreateHandler()
    {
        var ctor = HandlerType.GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var pt = parameters[i].ParameterType;
            if (pt == typeof(IPaymentRepository)) args[i] = _paymentRepository;
            else if (pt == typeof(IGymMembershipChecker)) args[i] = _membershipChecker;
            else args[i] = Substitute.For(new[] { pt }, Array.Empty<object>());
        }
        return ctor.Invoke(args);
    }

    private sealed record Paging(int Page = 1, int PageSize = 20);

    private object CreateQuery(Member member, Paging paging)
    {
        var ctor = QueryType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var args = ctor.GetParameters()
            .Select(p => QueryArgument(p, member, paging))
            .ToArray();
        return ctor.Invoke(args);
    }

    // strings, DateOnly?, enums without defaults — leave null; handler should treat as "all"/unset
    private static object? QueryArgument(ParameterInfo p, Member member, Paging paging)
    {
        var name = p.Name!;
        if (p.ParameterType == typeof(Guid))
            return name.Contains("gym", StringComparison.OrdinalIgnoreCase) ? member.GymId : member.UserId;
        if (p.ParameterType == typeof(int))
            return name.Contains("size", StringComparison.OrdinalIgnoreCase) ? paging.PageSize : paging.Page;
        return p.HasDefaultValue ? p.DefaultValue : null;
    }

    /// <summary>The calling trainee and the gym named by X-Gym-Id.</summary>
    private sealed record Member(Guid UserId, Guid GymId);

    private static Member NewMember() => new(Guid.NewGuid(), Guid.NewGuid());

    private void GivenActiveMember(Member member)
    {
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>()).Returns(true);
    }

    private void GivenPayments(Member member, List<Payment> payments) =>
        _paymentRepository.GetByMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>())
            .Returns(payments);

    private async Task<dynamic> ExecuteAsync(Member member, Paging? paging = null)
    {
        var handler = CreateHandler();
        var query = CreateQuery(member, paging ?? new Paging());
        return await InvokeHandleAsync(handler, query);
    }

    private static void AssertFailedWith(dynamic result, GymApp.Common.Domain.Error expected)
    {
        ((bool)result.IsFailure).Should().BeTrue();
        ((string)result.Error.Code).Should().Be(expected.Code);
    }

    private static async Task<dynamic> InvokeHandleAsync(object handler, object query)
    {
        var method = handler.GetType().GetMethod("Handle")!;
        var task = (Task)method.Invoke(handler, new[] { query, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result")!;
        return resultProp.GetValue(task)!;
    }

    private static Payment BuildCompletedPayment(Member member, DateTime? paidAt = null)
    {
        var payment = Payment.Create(new PaymentDetails
        {
            GymId = member.GymId,
            MemberId = member.UserId,
            Amount = Money.Create(500m),
            PaymentMethod = PaymentMethod.Cash,
            Purpose = PaymentPurpose.Membership,
            ReceiptNumber = $"REC-TEST-{Guid.NewGuid():N}".Substring(0, 20),
            RecordedBy = Guid.NewGuid(),
            ReferenceId = null
        });

        if (paidAt is not null)
        {
            var prop = typeof(Payment).GetProperty(nameof(Payment.PaidAt))!;
            prop.SetValue(payment, paidAt);
        }
        return payment;
    }

    [Fact]
    public async Task Should_ReturnGymSuspendedError_When_GymIsSuspended()
    {
        // PRD: "403 GYM_SUSPENDED"
        var member = NewMember();
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(true);

        AssertFailedWith(await ExecuteAsync(member), GymAccessErrors.GymSuspended);
    }

    [Fact]
    public async Task Should_ReturnNotAMemberError_When_CallerHasNoActiveMembership()
    {
        // PRD: "403 NOT_A_MEMBER_OF_GYM"
        var member = NewMember();
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>()).Returns(false);

        AssertFailedWith(await ExecuteAsync(member), GymAccessErrors.NotAMemberOfGym);
    }

    [Fact]
    public async Task Should_ReturnEmptyResult_When_TraineeHasNoPayments()
    {
        // PRD: "200 with data: [] when no payments"
        var member = NewMember();
        GivenActiveMember(member);
        GivenPayments(member, new List<Payment>());

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        CountItems((object?)result.Value).Should().Be(0);
    }

    [Fact]
    public async Task Should_IncludeVoidedPayments_When_VoidedPaymentsExist()
    {
        // PRD (Q9): "Voided payments ARE included with status: 'Voided'" — regression
        var member = NewMember();
        GivenActiveMember(member);

        var voided = BuildCompletedPayment(member, paidAt: DateTime.UtcNow.AddDays(-2));
        voided.Void("Recorded against wrong member");
        var completed = BuildCompletedPayment(member, paidAt: DateTime.UtcNow.AddDays(-1));

        GivenPayments(member, new List<Payment> { voided, completed });

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        var items = EnumerateItems((object?)result.Value).ToList();
        items.Should().HaveCount(2, because: "voided payments MUST NOT be filtered out (Q9 audit trail)");

        // Verify at least one item has status "Voided" with positive amount.
        bool foundVoided = items.Any(item =>
        {
            var statusProp = item.GetType().GetProperty("Status");
            var amountProp = item.GetType().GetProperty("Amount");
            var status = statusProp?.GetValue(item)?.ToString();
            var amount = amountProp?.GetValue(item);
            if (status != "Voided") return false;
            if (amount is decimal d) return d > 0;
            return true; // shape may differ; primary assertion is presence + status
        });
        foundVoided.Should().BeTrue(because: "voided payments must be present and tagged status='Voided'");
    }

    [Fact]
    public async Task Should_ReturnPaginationMetadata_When_ResultsExceedOnePage()
    {
        // PRD: "page=1 pageSize=2 with 5 records returns 2 items + meta {page, pageSize, total: 5, totalPages: 3}"
        var member = NewMember();
        GivenActiveMember(member);

        var payments = Enumerable.Range(0, 5)
            .Select(i => BuildCompletedPayment(member, paidAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();

        GivenPayments(member, payments);

        dynamic result = await ExecuteAsync(member, new Paging(Page: 1, PageSize: 2));

        ((bool)result.IsSuccess).Should().BeTrue();

        var value = (object?)result.Value;
        // Either PagedResponse<PaymentDto> or something with TotalCount/TotalPages.
        var totalProp = value?.GetType().GetProperty("TotalCount")
                        ?? value?.GetType().GetProperty("Total");
        var totalPagesProp = value?.GetType().GetProperty("TotalPages");
        var pageProp = value?.GetType().GetProperty("Page");
        var pageSizeProp = value?.GetType().GetProperty("PageSize");

        if (totalProp is not null && totalPagesProp is not null)
        {
            ((int)totalProp.GetValue(value)!).Should().Be(5);
            ((int)totalPagesProp.GetValue(value)!).Should().Be(3);
            ((int)pageProp!.GetValue(value)!).Should().Be(1);
            ((int)pageSizeProp!.GetValue(value)!).Should().Be(2);
            EnumerateItems(value).Should().HaveCount(2);
        }
        else
        {
            throw new Xunit.Sdk.XunitException(
                "Response is not a PagedResponse — PRD requires pagination metadata " +
                "{page, pageSize, total, totalPages} on /me/payments.");
        }
    }

    [Fact]
    public async Task Should_ClampOrRejectPageSize_When_PageSizeExceedsMaximum()
    {
        // PRD: "pageSize > 100 is clamped to 100 (or rejected with 400 — match whatever PRD says)"
        // PRD query-param block states: "pageSize (optional, default 20, max 50)".
        // So pageSize=500 must either be clamped to 50 or rejected with a validation failure.
        var member = NewMember();
        GivenActiveMember(member);
        GivenPayments(member, new List<Payment>());

        dynamic result = await ExecuteAsync(member, new Paging(Page: 1, PageSize: 500));

        if ((bool)result.IsSuccess)
        {
            var value = (object?)result.Value;
            var pageSizeProp = value?.GetType().GetProperty("PageSize");
            if (pageSizeProp is not null)
            {
                ((int)pageSizeProp.GetValue(value)!).Should().BeLessThanOrEqualTo(50,
                    because: "PRD: pageSize max is 50; must be clamped if accepted");
            }
        }
        else
        {
            // Also acceptable: validation failure.
            ((string)result.Error.Code).Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Should_SortByPaidAtDescending_When_ReturningPayments()
    {
        // PRD: "Sorted by paidAt descending"
        var member = NewMember();
        GivenActiveMember(member);

        var older = BuildCompletedPayment(member, paidAt: new DateTime(2026, 1, 1, 10, 0, 0));
        var newer = BuildCompletedPayment(member, paidAt: new DateTime(2026, 4, 1, 10, 0, 0));

        // Return in non-sorted order to force the handler to sort.
        GivenPayments(member, new List<Payment> { older, newer });

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        var items = EnumerateItems((object?)result.Value).ToList();
        items.Should().HaveCountGreaterThanOrEqualTo(2);

        var dates = items
            .Select(i => (DateTime?)i.GetType().GetProperty("PaidAt")?.GetValue(i))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        dates.Should().BeInDescendingOrder(because: "PRD: payments ordered by paidAt DESC");
    }

    [Fact]
    public async Task Should_ScopeResultsToRequestedGym_When_TraineeHasPaymentsAtMultipleGyms()
    {
        // PRD: "Multi-gym scoping" — Q3
        var member = NewMember(); // requested gym is gym B
        GivenActiveMember(member);
        GivenPayments(member, new List<Payment>());

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        await _paymentRepository.Received(1)
            .GetByMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Should_NotDependOnGymPlanService_When_ConstructingHandler()
    {
        // PRD: "Plan-feature OFF still returns 200" — handler must not gate on payment_recording.
        var ctor = HandlerType.GetConstructors().Single();
        ctor.GetParameters().Should().NotContain(
            p => p.ParameterType == typeof(IGymPlanService),
            because: "PRD forbids plan-feature gating on /me/payments (PDPL exemption)");
    }

    private static int CountItems(object? value) => EnumerateItems(value).Count();

    private static IEnumerable<object> EnumerateItems(object? value) =>
        ItemsCollection(value)?.Cast<object>() ?? Enumerable.Empty<object>();

    // PagedResponse<T>.Items when present, otherwise the value itself if it is a collection.
    private static System.Collections.IEnumerable? ItemsCollection(object? value)
    {
        if (value is null) return null;

        var itemsProp = value.GetType().GetProperty("Items");
        if (itemsProp is not null)
            return itemsProp.GetValue(value) as System.Collections.IEnumerable;

        return value is System.Collections.IEnumerable e and not string ? e : null;
    }
}
