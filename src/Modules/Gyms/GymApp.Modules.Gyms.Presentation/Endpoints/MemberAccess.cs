using System.Security.Claims;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

/// <summary>Who may call a <c>/gyms/{gymId}/members…</c> endpoint, besides platform admins.</summary>
internal enum MemberAccessLevel
{
    /// <summary>Gym staff (owner, manager, receptionist) of this gym.</summary>
    Staff,

    /// <summary>Gym staff, or a trainer of this gym.</summary>
    StaffOrTrainer,

    /// <summary>Gym staff, a trainer of this gym, or the member the route is about.</summary>
    StaffTrainerOrSelf,
}

/// <summary>
/// Authorization for the gym member endpoints. They used to require only an authenticated
/// user, so any signed-in principal — including another trainee of the gym — could list
/// the roster, read or edit other members, freeze them, and read trainers' private notes
/// and intake (health) data.
///
/// Rules: platform admins always pass. Everyone else must be scoped to the gym in the
/// route and hold a role the endpoint's <see cref="MemberAccessLevel"/> allows. Per the
/// staff permission matrix (docs/01) every staff role may view, add and edit members, so
/// staff are not split further here.
/// </summary>
internal static class MemberAccess
{
    public static bool IsAllowed(ClaimsPrincipal user, Guid gymId, Guid? memberId, MemberAccessLevel level)
    {
        if (IsPlatformAdmin(user))
            return true;

        if (!IsScopedToGym(user, gymId))
            return false;

        if (IsStaff(user))
            return true;

        return level switch
        {
            MemberAccessLevel.StaffOrTrainer => IsTrainer(user),
            MemberAccessLevel.StaffTrainerOrSelf => IsTrainer(user) || IsSelf(user, memberId),
            _ => false,
        };
    }

    // The caller is the member the route is about.
    private static bool IsSelf(ClaimsPrincipal user, Guid? memberId) =>
        memberId.HasValue && TryGetUserId(user, out var userId) && userId == memberId.Value;

    public static bool IsPlatformAdmin(ClaimsPrincipal user) =>
        !string.IsNullOrEmpty(user.FindFirstValue("adminRole"));

    public static bool IsStaff(ClaimsPrincipal user) =>
        !string.IsNullOrEmpty(user.FindFirstValue("staffRole"));

    public static bool IsTrainer(ClaimsPrincipal user) =>
        string.Equals(
            user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role"),
            "trainer",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>A trainer who is neither gym staff nor a platform admin.</summary>
    public static bool IsTrainerOnly(ClaimsPrincipal user) =>
        IsTrainer(user) && !IsStaffOrAdmin(user);

    private static bool IsStaffOrAdmin(ClaimsPrincipal user) =>
        IsStaff(user) || IsPlatformAdmin(user);

    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
            out userId);

    /// <summary>
    /// The token's gym claim must match the route. An owner who has not selected a gym
    /// carries no gym claim; TenantResolutionMiddleware has already verified they own the
    /// route's gym (and 404s otherwise), so that case is accepted here.
    /// </summary>
    private static bool IsScopedToGym(ClaimsPrincipal user, Guid gymId)
    {
        var gymClaim = user.FindFirstValue("gymId");
        if (!string.IsNullOrEmpty(gymClaim))
            return Guid.TryParse(gymClaim, out var tokenGymId) && tokenGymId == gymId;

        return string.Equals(user.FindFirstValue("staffRole"), "owner", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Rejects the request with 403 unless the caller satisfies <paramref name="level"/>
    /// for the route's <c>gymId</c> (and <c>memberId</c>, when present).
    /// </summary>
    public static RouteHandlerBuilder RequireMemberAccess(this RouteHandlerBuilder builder, MemberAccessLevel level) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var gymId = RouteGuid(http, "gymId");
            if (gymId is null || !IsAllowed(http.User, gymId.Value, RouteGuid(http, "memberId"), level))
            {
                return ApiResults.Problem(
                    "FORBIDDEN", "You are not allowed to access this gym's member data.", 403);
            }

            return await next(context);
        });

    private static Guid? RouteGuid(HttpContext http, string key) =>
        Guid.TryParse(http.Request.RouteValues[key]?.ToString(), out var value) ? value : null;
}
