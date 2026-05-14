using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

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

    public static string? GetRole(HttpContext httpContext)
    {
        var principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return principal.FindFirstValue(ClaimTypes.Role)
            ?? principal.FindFirstValue("role");
    }

    public static Guid? TryGetGymId(HttpContext httpContext)
    {
        var raw = httpContext.User.FindFirstValue("gymId");
        return Guid.TryParse(raw, out var gymId) ? gymId : null;
    }
}
