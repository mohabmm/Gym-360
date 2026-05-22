using System.Reflection;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Application.Billing;

/// <summary>
/// Unit tests for GET /api/v1/billing/me/packages per PRD_TRAINEE_ME_ENDPOINTS.md.
/// One test method per PRD acceptance criterion.
///
/// At test-authoring time the backend agent had not yet landed
/// GetMyPackagesQueryHandler. We use reflection to locate the handler once it
/// lands in GymApp.Modules.Billing.Application; if the type is missing the
/// tests fail with a clear "not implemented" message. After the backend agent
/// merges their PR these tests run green.
/// </summary>
public class GetMyPackagesQueryHandlerTests
{
    private readonly ISessionPackageRepository _packageRepository = Substitute.For<ISessionPackageRepository>();
    private readonly IGymMembershipChecker _membershipChecker = Substitute.For<IGymMembershipChecker>();

    private const string HandlerTypeName =
        "GymApp.Modules.Billing.Application.Queries.SessionPackages.GetMyPackagesQueryHandler";
    private const string QueryTypeName =
        "GymApp.Modules.Billing.Application.Queries.SessionPackages.GetMyPackagesQuery";

    private static readonly Assembly BillingAppAssembly =
        typeof(GymApp.Modules.Billing.Application.DTOs.PaymentDto).Assembly;

    private static Type HandlerType => BillingAppAssembly.GetType(HandlerTypeName)
        ?? throw new Xunit.Sdk.XunitException(
            $"Handler type '{HandlerTypeName}' not found. The backend agent has not yet implemented " +
            "GET /api/v1/billing/me/packages. This test will run green once the handler lands.");

    private static Type QueryType => BillingAppAssembly.GetType(QueryTypeName)
        ?? throw new Xunit.Sdk.XunitException(
            $"Query type '{QueryTypeName}' not found. The backend agent has not yet implemented " +
            "GET /api/v1/billing/me/packages.");

    private object CreateHandler()
    {
        var ctor = HandlerType.GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var pt = parameters[i].ParameterType;
            if (pt == typeof(ISessionPackageRepository)) args[i] = _packageRepository;
            else if (pt == typeof(IGymMembershipChecker)) args[i] = _membershipChecker;
            else args[i] = Substitute.For(new[] { pt }, Array.Empty<object>());
        }
        return ctor.Invoke(args);
    }

    private object CreateQuery(Guid userId, Guid gymId, string? status = null, int page = 1, int pageSize = 20)
    {
        var ctor = QueryType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.ParameterType == typeof(Guid))
            {
                args[i] = p.Name!.Contains("gym", StringComparison.OrdinalIgnoreCase) ? gymId : userId;
            }
            else if (p.ParameterType == typeof(string))
            {
                args[i] = status;
            }
            else if (p.ParameterType == typeof(int))
            {
                args[i] = p.Name!.Contains("size", StringComparison.OrdinalIgnoreCase) ? pageSize : page;
            }
            else if (p.HasDefaultValue)
            {
                args[i] = p.DefaultValue;
            }
            else
            {
                args[i] = null;
            }
        }
        return ctor.Invoke(args);
    }

    private static async Task<dynamic> InvokeHandleAsync(object handler, object query)
    {
        var method = handler.GetType().GetMethod("Handle")!;
        var task = (Task)method.Invoke(handler, new[] { query, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result")!;
        return resultProp.GetValue(task)!;
    }

    private static SessionPackage BuildActivePackage(Guid gymId, Guid memberId) =>
        SessionPackage.Create(
            gymId: gymId,
            trainerId: Guid.NewGuid(),
            memberId: memberId,
            templateId: Guid.NewGuid(),
            name: "10 Sessions — PT",
            totalSessions: 10,
            pricePerSession: Money.Create(500m),
            totalPrice: Money.Create(5000m),
            assignedBy: Guid.NewGuid(),
            expiresAt: DateTime.UtcNow.AddMonths(3));

    [Fact]
    public async Task Should_ReturnGymSuspendedError_When_GymIsSuspended()
    {
        // PRD: "403 GYM_SUSPENDED when gym is suspended"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymId);

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsFailure).Should().BeTrue();
        ((string)result.Error.Code).Should().Be(GymAccessErrors.GymSuspended.Code);
    }

    [Fact]
    public async Task Should_ReturnNotAMemberError_When_CallerHasNoActiveMembership()
    {
        // PRD: "403 NOT_A_MEMBER_OF_GYM when not a member of X-Gym-Id"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymId);

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsFailure).Should().BeTrue();
        ((string)result.Error.Code).Should().Be(GymAccessErrors.MemberRoleRequired.Code);
    }

    [Fact]
    public async Task Should_ReturnEmptyResult_When_TraineeHasNoPackages()
    {
        // PRD: "Returns 200 with data: [] when trainee has no packages"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);
        _packageRepository.GetByMemberAsync(gymId, userId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage>());

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymId);

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsSuccess).Should().BeTrue();
        CountItems((object?)result.Value).Should().Be(0);
    }

    [Fact]
    public async Task Should_ReturnOnlyCallersOwnPackages_When_QueryingByAuthenticatedUser()
    {
        // PRD: "Returns only the trainee's own packages (not other members')"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var myPackage = BuildActivePackage(gymId, userId);
        _packageRepository.GetByMemberAsync(gymId, userId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage> { myPackage });

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymId);

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsSuccess).Should().BeTrue();
        await _packageRepository.Received(1)
            .GetByMemberAsync(gymId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ScopeResultsToRequestedGym_When_TraineeHasPackagesAtMultipleGyms()
    {
        // PRD: "Returns packages scoped to X-Gym-Id only (multi-gym test)" — Q3
        var userId = Guid.NewGuid();
        var gymB = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymB, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymB, userId, Arg.Any<CancellationToken>()).Returns(true);
        _packageRepository.GetByMemberAsync(gymB, userId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage>());

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymB);

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsSuccess).Should().BeTrue();
        await _packageRepository.Received(1)
            .GetByMemberAsync(gymB, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Should_NotDependOnGymPlanService_When_ConstructingHandler()
    {
        // PRD: "Does NOT gate on session_packages feature (PDPL — trainee's own data)."
        var ctor = HandlerType.GetConstructors().Single();
        ctor.GetParameters().Should().NotContain(
            p => p.ParameterType == typeof(IGymPlanService),
            because: "PRD forbids plan-feature gating on /me/packages (PDPL exemption)");
    }

    [Fact]
    public async Task Should_IncludeActiveAndExpiredPackages_When_BothStatusesExist()
    {
        // PRD: "Includes both Active and Expired/Completed packages"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var active = BuildActivePackage(gymId, userId);
        var expired = BuildActivePackage(gymId, userId);
        expired.Expire();

        _packageRepository.GetByMemberAsync(gymId, userId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionPackage> { active, expired });

        var handler = CreateHandler();
        var query = CreateQuery(userId, gymId, status: "All");

        dynamic result = await InvokeHandleAsync(handler, query);

        ((bool)result.IsSuccess).Should().BeTrue();
        CountItems((object?)result.Value).Should().Be(2,
            because: "PRD requires status=All to include both Active and Expired packages");
    }

    private static int CountItems(object? value)
    {
        if (value is null) return 0;

        // PagedResponse<T>.Items
        var itemsProp = value.GetType().GetProperty("Items");
        if (itemsProp is not null)
        {
            var items = itemsProp.GetValue(value) as System.Collections.IEnumerable;
            return items?.Cast<object>().Count() ?? 0;
        }

        if (value is System.Collections.IEnumerable e && value is not string)
            return e.Cast<object>().Count();

        return 0;
    }
}
