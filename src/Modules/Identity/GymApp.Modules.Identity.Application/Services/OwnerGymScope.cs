using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Application.Services;

/// <summary>Which gyms an Owner principal may scope their token to (select-gym and refresh).</summary>
internal static class OwnerGymScope
{
    public static bool IsOwner(User user) => user.Role == UserRole.GymOwner && user.OwnerId.HasValue;

    /// <summary>
    /// The owner's gym, or null when the user is not an owner or does not own it.
    /// <see cref="GymRegistration.OwnerId"/> may be the Owner aggregate Id (legacy bootstrap) or
    /// the User Id (admin /gyms flow); either counts.
    /// </summary>
    public static async Task<GymRegistration?> FindOwnedGymAsync(
        IGymRegistry registry, User user, Guid gymId, CancellationToken cancellationToken)
    {
        if (!IsOwner(user))
            return null;

        var registration = await registry.FindAsync(gymId, cancellationToken);
        return registration is not null && Owns(user, registration) ? registration : null;
    }

    public static bool Owns(User user, GymRegistration registration) =>
        registration.OwnerId == user.OwnerId || registration.OwnerId == user.Id;
}
