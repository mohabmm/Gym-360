using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

/// <summary>
/// Extracts the authenticated user id from the JWT (sub claim). Used by PDPL and
/// profile endpoints to guarantee that clients cannot target another user's data.
/// </summary>
internal static class UserContext
{
    // ASP.NET's JwtBearer handler typically maps "sub" -> ClaimTypes.NameIdentifier.
    // We check both, plus the raw "sub", to be defensive across handler configs.
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

    public static string? GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString();
}
