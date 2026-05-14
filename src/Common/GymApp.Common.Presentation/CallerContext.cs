using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GymApp.Common.Presentation;

/// <summary>
/// Helper to extract the authenticated caller's user id from the JWT.
/// Used by endpoints that previously trusted client-supplied caller-self
/// identity fields in the request body.
/// </summary>
public static class CallerContext
{
    public static bool TryGetUserId(HttpContext httpContext, out Guid userId)
    {
        userId = Guid.Empty;

        var principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        var raw =
            principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("nameid")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        return Guid.TryParse(raw, out userId);
    }

    public static bool TryGetGymId(HttpContext httpContext, out Guid gymId)
    {
        gymId = Guid.Empty;
        var raw = httpContext.User.FindFirstValue("gymId");
        return Guid.TryParse(raw, out gymId);
    }
}
