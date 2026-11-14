using System.Security.Claims;

namespace GymApp.Modules.Billing.Presentation.Endpoints.SessionPackages;

/// <summary>
/// Who may list a gym's session packages (GET /billing/packages). It used to accept any
/// signed-in user, so anyone could read any gym's packages (members, coaches, prices).
///
/// Platform admins: any gym. Gym staff: their own gym. Trainers: their own gym, and only
/// packages they coach (the trainer filter is forced to themselves). Everyone else: denied.
/// An owner with no gym selected carries no gym claim; TenantResolutionMiddleware has
/// already checked they own the requested gym, so that case is accepted.
/// </summary>
internal static class PackageListAccess
{
    /// <summary>
    /// Whether the caller may list <paramref name="gymId"/>'s packages, and the
    /// trainer filter to apply (the requested one, or the caller's own id for a trainer).
    /// </summary>
    public static (bool Allowed, Guid? TrainerId) Resolve(ClaimsPrincipal user, Guid gymId, Guid? requestedTrainerId)
    {
        if (!string.IsNullOrEmpty(user.FindFirstValue("adminRole")))
            return (true, requestedTrainerId);

        if (!IsScopedToGym(user, gymId))
            return (false, null);

        if (!string.IsNullOrEmpty(user.FindFirstValue("staffRole")))
            return (true, requestedTrainerId);

        return IsTrainer(user) && TryGetUserId(user, out var self)
            ? (true, self)
            : (false, null);
    }

    private static bool IsScopedToGym(ClaimsPrincipal user, Guid gymId)
    {
        var gymClaim = user.FindFirstValue("gymId");
        if (!string.IsNullOrEmpty(gymClaim))
            return Guid.TryParse(gymClaim, out var tokenGymId) && tokenGymId == gymId;

        return string.Equals(user.FindFirstValue("staffRole"), "owner", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrainer(ClaimsPrincipal user) =>
        string.Equals(
            user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role"),
            "trainer",
            StringComparison.OrdinalIgnoreCase);

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out userId);
}
