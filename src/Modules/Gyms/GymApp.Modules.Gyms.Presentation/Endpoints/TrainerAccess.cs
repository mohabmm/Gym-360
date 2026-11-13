using System.Security.Claims;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

/// <summary>Who may change a gym's trainer roster, besides platform admins.</summary>
internal enum TrainerRosterAction
{
    /// <summary>Add a coach to the gym: any staff of the gym (owner, manager, receptionist).</summary>
    Add,

    /// <summary>Approve or reject a trainer sign-up: owner or manager of the gym.</summary>
    Decide,
}

/// <summary>
/// Authorization for <c>POST /gyms/{gymId}/trainers…</c>. These used to require only a
/// signed-in user, so anyone — even a trainee of another gym — could add, approve or
/// reject trainers at any gym. The caller must now be scoped to the route's gym.
/// </summary>
internal static class TrainerAccess
{
    public static bool IsAllowed(ClaimsPrincipal user, Guid gymId, TrainerRosterAction action)
    {
        if (MemberAccess.IsPlatformAdmin(user))
            return true;

        if (!MemberAccess.IsStaff(user) || !MemberAccess.IsScopedToGym(user, gymId))
            return false;

        return action == TrainerRosterAction.Add || IsOwnerOrManager(user);
    }

    private static bool IsOwnerOrManager(ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue("staffRole"), "owner", StringComparison.OrdinalIgnoreCase) ||
        user.FindAll("staffSubRole").Any(c =>
            string.Equals(c.Value, "Owner", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Value, "Manager", StringComparison.OrdinalIgnoreCase));

    /// <summary>Rejects the request with 403 unless the caller may perform <paramref name="action"/>.</summary>
    public static RouteHandlerBuilder RequireTrainerRosterAccess(this RouteHandlerBuilder builder, TrainerRosterAction action) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var gymId = MemberAccess.RouteGuid(http, "gymId");
            if (gymId is null || !IsAllowed(http.User, gymId.Value, action))
            {
                return ApiResults.Problem(
                    "FORBIDDEN", "You are not allowed to change this gym's trainers.", 403);
            }

            return await next(context);
        });
}
