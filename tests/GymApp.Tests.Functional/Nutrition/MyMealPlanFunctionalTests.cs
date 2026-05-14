using System.Net;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Nutrition;

/// <summary>
/// HTTP-level functional tests for GET /api/v1/nutrition/me/meal-plan.
/// Exercises the PRD error catalogue at the wire protocol level.
/// </summary>
public sealed class MyMealPlanFunctionalTests : BaseFunctionalTest
{
    public MyMealPlanFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    private const string Path = "api/v1/nutrition/me/meal-plan";

    [Fact]
    public async Task Should_Return401_When_NoJwtProvided()
    {
        var response = await AnonymousClient.GetAsync(Path);
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Return400_When_XGymIdHeaderMissing()
    {
        // PRD: 400 GYM_CONTEXT_REQUIRED when X-Gym-Id header missing.
        var client = CreateTraineeClient();

        var response = await client.GetAsync(Path);

        AssertStatusCode(response, HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("GYM_CONTEXT_REQUIRED");
    }

    [Fact]
    public async Task Should_Return403NotAMember_When_UserIsNotMemberOfGym()
    {
        // The gymId is random — the authenticated user has no membership record for it.
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient();
        client.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        var response = await client.GetAsync(Path);

        AssertStatusCode(response, HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("NOT_A_MEMBER_OF_GYM",
            because: "PRD: unknown/unjoined gym must return NOT_A_MEMBER_OF_GYM");
    }
}
