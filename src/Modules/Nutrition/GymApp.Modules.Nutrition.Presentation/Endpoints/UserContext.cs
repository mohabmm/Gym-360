using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

/// <summary>
/// Resolves the authenticated user id from the JWT (sub claim). Mirrors the
/// helper in the Identity module — kept module-local rather than promoted to
/// Common.Presentation to avoid widening shared surface area for a trivial
/// two-line lookup.
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
}
