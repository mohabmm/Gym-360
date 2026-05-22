using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Modules.Nutrition.Application.Queries.GetMyMealPlan;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Tests.Application.Nutrition;

/// <summary>
/// Unit tests for GET /api/v1/nutrition/me/meal-plan per PRD_TRAINEE_ME_ENDPOINTS.md.
/// One test method per PRD acceptance criterion.
/// </summary>
public class GetMyMealPlanQueryHandlerTests
{
    private readonly IMealPlanRepository _mealPlanRepository = Substitute.For<IMealPlanRepository>();
    private readonly IGymMembershipChecker _membershipChecker = Substitute.For<IGymMembershipChecker>();
    private readonly GetMyMealPlanQueryHandler _sut;

    public GetMyMealPlanQueryHandlerTests()
    {
        _sut = new GetMyMealPlanQueryHandler(_mealPlanRepository, _membershipChecker);
    }

    private static MealPlan BuildAssignedPlan(
        Guid clientId,
        Guid gymId,
        DateTime? assignedAt = null,
        bool isTemplate = false)
    {
        var plan = MealPlan.Create(
            trainerId: Guid.NewGuid(),
            gymId: gymId,
            name: "Test Plan",
            description: null,
            dailyCalorieTarget: 2200,
            macroTargets: new MacroTargets(165, 220, 75),
            isTemplate: isTemplate);

        if (!isTemplate)
        {
            plan.AssignToClient(clientId);
        }

        if (assignedAt is not null)
        {
            // Use reflection to set UpdatedAt so we can verify "most recent" ordering
            var prop = typeof(MealPlan).GetProperty(nameof(MealPlan.UpdatedAt))!;
            prop.SetValue(plan, assignedAt.Value);
        }

        return plan;
    }

    [Fact]
    public async Task Should_ReturnGymSuspendedError_When_GymIsSuspended()
    {
        // PRD: "Returns 403 GYM_SUSPENDED when gym is suspended"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GymAccessErrors.GymSuspended.Code);
    }

    [Fact]
    public async Task Should_ReturnNotAMemberError_When_CallerHasNoActiveMembership()
    {
        // PRD: "Returns 403 NOT_A_MEMBER_OF_GYM when caller has no active membership at gymId"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GymAccessErrors.NotAMemberOfGym.Code);
    }

    [Fact]
    public async Task Should_ReturnNullData_When_TraineeHasNoAssignedMealPlan()
    {
        // PRD: "Returns 200 with data: null when trainee has no assigned meal plan"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);
        _mealPlanRepository.GetByClientAsync(userId, gymId, Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan>());

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnMostRecentlyAssignedPlan_When_MultipleNonTemplatePlansExist()
    {
        // PRD: "Returns the most-recently-assigned non-template plan (not a template, not an older one)"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var older = BuildAssignedPlan(userId, gymId, assignedAt: new DateTime(2026, 1, 1));
        var newer = BuildAssignedPlan(userId, gymId, assignedAt: new DateTime(2026, 4, 10));
        var template = BuildAssignedPlan(Guid.NewGuid(), gymId, isTemplate: true);

        _mealPlanRepository.GetByClientAsync(userId, gymId, Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan> { older, template, newer });

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(newer.Id);
        result.Value.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public async Task Should_NotCallFeatureGate_When_HandlingQuery()
    {
        // PRD: "Does NOT call IGymPlanService.IsFeatureEnabledAsync — meal_plans is irrelevant here".
        // Regression: if someone adds gating later, this test fails.
        // We verify by proving the handler does NOT require IGymPlanService in its ctor
        // by calling it successfully without one (constructor only takes repo + checker).
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);
        _mealPlanRepository.GetByClientAsync(userId, gymId, Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan>());

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Assertion by construction: GetMyMealPlanQueryHandler has no IGymPlanService dependency.
        var ctor = typeof(GetMyMealPlanQueryHandler).GetConstructors().Single();
        ctor.GetParameters().Should().NotContain(
            p => p.ParameterType == typeof(IGymPlanService),
            because: "PRD forbids plan-feature gating on trainee /me reads (PDPL)");
    }

    [Fact]
    public async Task Should_ScopeResultsToRequestedGym_When_TraineeHasPlansInMultipleGyms()
    {
        // PRD: "Multi-gym: trainee with plans at Gym A and Gym B with X-Gym-Id=GymB returns Gym B's plan only"
        // Repository is already called with a specific gymId; this test asserts the handler passes
        // the correct gymId through.
        var userId = Guid.NewGuid();
        var gymB = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymB, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymB, userId, Arg.Any<CancellationToken>()).Returns(true);

        var planAtGymB = BuildAssignedPlan(userId, gymB, assignedAt: DateTime.UtcNow);
        _mealPlanRepository.GetByClientAsync(userId, gymB, Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan> { planAtGymB });

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mealPlanRepository.Received(1).GetByClientAsync(userId, gymB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotReturnTemplatePlan_When_OnlyTemplateExistsForClient()
    {
        // PRD: "Never returns template plans (IsTemplate == true)"
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        _membershipChecker.IsGymSuspendedAsync(gymId, Arg.Any<CancellationToken>()).Returns(false);
        _membershipChecker.IsActiveMemberAsync(gymId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var template = BuildAssignedPlan(userId, gymId, isTemplate: true);
        _mealPlanRepository.GetByClientAsync(userId, gymId, Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan> { template });

        var result = await _sut.Handle(new GetMyMealPlanQuery(userId, gymId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull(because: "template plans must never be returned from /me/meal-plan");
    }
}
