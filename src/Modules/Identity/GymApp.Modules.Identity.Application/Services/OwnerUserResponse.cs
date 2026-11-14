using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Application.Services;

/// <summary>The user payload returned with an owner token scoped to one of their gyms.</summary>
internal static class OwnerUserResponse
{
    public static UserResponse For(User user, GymRegistration gym) =>
        new(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            gym.GymId,
            gym.Name,
            PrincipalType: "Owner",
            OwnerId: user.OwnerId);
}
