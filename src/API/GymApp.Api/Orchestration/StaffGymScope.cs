using System.Security.Claims;
using GymApp.Common.Domain;

namespace GymApp.Api.Orchestration;

/// <summary>Reads the gym scope a staff JWT carries (the <c>gymId</c> claim).</summary>
internal static class StaffGymScope
{
    public static readonly Error Missing = new(
        "Auth.GymContextMissing",
        "Staff JWT is missing a gym scope. Re-login or call /identity/select-gym.");

    public static bool TryGetGymId(ClaimsPrincipal user, out Guid gymId) =>
        Guid.TryParse(user.FindFirstValue("gymId"), out gymId) && gymId != Guid.Empty;
}
