using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Billing.Presentation.Endpoints;

/// <summary>The selected gym for trainer/trainee PT endpoints: the token's gym first, then X-Gym-Id.</summary>
internal static class GymContext
{
    public static Guid? Resolve(HttpContext httpContext, Guid? headerGymId)
    {
        var gymId = UserContext.TryGetGymId(httpContext) ?? headerGymId;
        return gymId is { } id && id != Guid.Empty ? id : null;
    }

    public static IResult Required() =>
        ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (token gym or X-Gym-Id header).", StatusCodes.Status400BadRequest);
}
