using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.TrainingPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>Builds the <see cref="PlanActor"/> from the token and holds the plan endpoints' shared policies.</summary>
internal static class PlanEndpointSupport
{
    /// <summary>Coaches, plus the gym's owner and managers (staff web gym templates).</summary>
    public static void CoachOrGymManager(AuthorizationPolicyBuilder policy) =>
        policy.RequireAuthenticatedUser().RequireAssertion(ctx => IsTrainer(ctx.User) || IsGymManager(ctx.User));

    public static bool TryGetActor(HttpContext httpContext, out PlanActor actor, out IResult? problem)
    {
        actor = default!;
        problem = null;
        if (!UserContext.TryGetUserId(httpContext, out var userId))
        {
            problem = Results.Unauthorized();
            return false;
        }
        var gymId = UserContext.TryGetGymId(httpContext);
        if (gymId is null || gymId == Guid.Empty)
        {
            problem = ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", StatusCodes.Status400BadRequest);
            return false;
        }
        actor = new PlanActor(gymId.Value, userId, IsGymManager(httpContext.User));
        return true;
    }

    public static bool IsTrainer(ClaimsPrincipal user) => user.IsInRole("trainer");

    // Same rule as the gym_owner_or_manager policy: owner principals, or staff whose gym
    // sub-role is Owner or Manager.
    private static bool IsGymManager(ClaimsPrincipal user) =>
        user.HasClaim("staffRole", "owner")
        || user.HasClaim(c => c.Type == "staffSubRole"
            && (string.Equals(c.Value, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Value, "Manager", StringComparison.OrdinalIgnoreCase)));

    public static IResult ToResult<T>(GymApp.Common.Domain.Result<T> result) =>
        result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
}
