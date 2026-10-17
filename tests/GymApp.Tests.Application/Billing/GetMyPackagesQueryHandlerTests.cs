using System.Reflection;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Identity.Shared;
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
    private readonly IUserDisplayLookup _userDisplayLookup = Substitute.For<IUserDisplayLookup>();

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
            else if (pt == typeof(IUserDisplayLookup)) args[i] = _userDisplayLookup;
            else args[i] = Substitute.For(new[] { pt }, Array.Empty<object>());
        }
        return ctor.Invoke(args);
    }

    /// <summary>The calling trainee and the gym named by X-Gym-Id.</summary>
    private sealed record Member(Guid UserId, Guid GymId);

    private static Member NewMember() => new(Guid.NewGuid(), Guid.NewGuid());

    private object CreateQuery(Member member, string? status)
    {
        var ctor = QueryType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var args = ctor.GetParameters()
            .Select(p => QueryArgument(p, member, status))
            .ToArray();
        return ctor.Invoke(args);
    }

    // Guids map to gym/user by name, strings to the status filter, ints to page 1 / size 20.
    private static object? QueryArgument(ParameterInfo p, Member member, string? status)
    {
        var name = p.Name!;
        if (p.ParameterType == typeof(Guid))
            return name.Contains("gym", StringComparison.OrdinalIgnoreCase) ? member.GymId : member.UserId;
        if (p.ParameterType == typeof(string))
            return status;
        if (p.ParameterType == typeof(int))
            return name.Contains("size", StringComparison.OrdinalIgnoreCase) ? 20 : 1;
        return p.HasDefaultValue ? p.DefaultValue : null;
    }

    private void GivenActiveMember(Member member)
    {
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>()).Returns(true);
    }

    private void GivenPackages(Member member, List<SessionPackage> packages) =>
        _packageRepository.GetByMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>())
            .Returns(packages);

    private async Task<dynamic> ExecuteAsync(Member member, string? status = null)
    {
        var handler = CreateHandler();
        var query = CreateQuery(member, status);
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

    private static SessionPackage BuildActivePackage(Member member) =>
        SessionPackage.Create(new SessionPackageDetails
        {
            GymId = member.GymId,
            TrainerId = Guid.NewGuid(),
            MemberId = member.UserId,
            TemplateId = Guid.NewGuid(),
            Name = "10 Sessions — PT",
            TotalSessions = 10,
            PricePerSession = Money.Create(500m),
            TotalPrice = Money.Create(5000m),
            AssignedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        });

    [Fact]
    public async Task Should_ReturnGymSuspendedError_When_GymIsSuspended()
    {
        // PRD: "403 GYM_SUSPENDED when gym is suspended"
        var member = NewMember();
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(true);

        AssertFailedWith(await ExecuteAsync(member), GymAccessErrors.GymSuspended);
    }

    [Fact]
    public async Task Should_ReturnNotAMemberError_When_CallerHasNoActiveMembership()
    {
        // PRD: "403 NOT_A_MEMBER_OF_GYM when not a member of X-Gym-Id"
        var member = NewMember();
        _membershipChecker.IsGymSuspendedAsync(member.GymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>()).Returns(false);

        AssertFailedWith(await ExecuteAsync(member), GymAccessErrors.NotAMemberOfGym);
    }

    [Fact]
    public async Task Should_ReturnEmptyResult_When_TraineeHasNoPackages()
    {
        // PRD: "Returns 200 with data: [] when trainee has no packages"
        var member = NewMember();
        GivenActiveMember(member);
        GivenPackages(member, new List<SessionPackage>());

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        CountItems((object?)result.Value).Should().Be(0);
    }

    [Fact]
    public async Task Should_ReturnOnlyCallersOwnPackages_When_QueryingByAuthenticatedUser()
    {
        // PRD: "Returns only the trainee's own packages (not other members')"
        var member = NewMember();
        GivenActiveMember(member);

        var myPackage = BuildActivePackage(member);
        GivenPackages(member, new List<SessionPackage> { myPackage });

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        await _packageRepository.Received(1)
            .GetByMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ScopeResultsToRequestedGym_When_TraineeHasPackagesAtMultipleGyms()
    {
        // PRD: "Returns packages scoped to X-Gym-Id only (multi-gym test)" — Q3
        var member = NewMember(); // requested gym is gym B
        GivenActiveMember(member);
        GivenPackages(member, new List<SessionPackage>());

        dynamic result = await ExecuteAsync(member);

        ((bool)result.IsSuccess).Should().BeTrue();
        await _packageRepository.Received(1)
            .GetByMemberAsync(member.GymId, member.UserId, Arg.Any<CancellationToken>());
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
        var member = NewMember();
        GivenActiveMember(member);

        var active = BuildActivePackage(member);
        var expired = BuildActivePackage(member);
        expired.Expire();

        GivenPackages(member, new List<SessionPackage> { active, expired });

        dynamic result = await ExecuteAsync(member, status: "All");

        ((bool)result.IsSuccess).Should().BeTrue();
        CountItems((object?)result.Value).Should().Be(2,
            because: "PRD requires status=All to include both Active and Expired packages");
    }

    [Fact]
    public async Task Should_ResolveTrainerName_When_UserDisplayLookupReturnsIt()
    {
        // Trainer display name is resolved cross-module via IUserDisplayLookup
        // (Identity .Shared). Previously this field was always null.
        var member = NewMember();
        GivenActiveMember(member);

        var package = BuildActivePackage(member);
        var trainerId = package.TrainerId;
        GivenPackages(member, new List<SessionPackage> { package });

        IReadOnlyDictionary<Guid, UserDisplay> names = new Dictionary<Guid, UserDisplay>
        {
            [trainerId] = new UserDisplay(trainerId, "Coach Karim", null)
        };
        _userDisplayLookup.GetDisplayNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(names);

        dynamic result = await ExecuteAsync(member, status: "All");

        ((bool)result.IsSuccess).Should().BeTrue();
        FirstItemTrainerName((object?)result.Value).Should().Be("Coach Karim");
    }

    private static string? FirstItemTrainerName(object? value)
    {
        var itemsProp = value?.GetType().GetProperty("Items");
        if (itemsProp?.GetValue(value) is not System.Collections.IEnumerable items)
            return null;
        var first = items.Cast<object>().FirstOrDefault();
        return first?.GetType().GetProperty("TrainerName")?.GetValue(first) as string;
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
