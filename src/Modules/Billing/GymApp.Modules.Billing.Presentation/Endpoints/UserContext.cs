using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Billing.Presentation.Endpoints;

/// <summary>
/// Resolves the authenticated user id from the JWT (sub claim). Kept
/// module-local (mirrors the Identity module helper) to avoid widening the
/// Common.Presentation surface area.
/// </summary>
internal static class UserContext
{
    public static bool TryGetUserId(HttpContext httpContext, out Guid userId)
    {
        userId = Guid.Empty;

        var principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        var raw =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("nameid");

        return Guid.TryParse(raw, out userId);
    }

    public static Guid? TryGetGymId(HttpContext httpContext)
    {
        var raw = httpContext.User.FindFirstValue("gymId");
        return Guid.TryParse(raw, out var gymId) ? gymId : null;
    }
}
