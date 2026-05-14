using GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Contracts;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issue a token scoped to a specific gym. Used by the Owner select-gym flow,
    /// where the initial login token had no gym_id and the Owner subsequently picked
    /// one of the gyms returned in <c>UserResponse.OwnedGyms</c>.
    /// </summary>
    Task<string> GenerateAccessTokenForGymAsync(User user, Guid gymId, string schemaPrefix, CancellationToken cancellationToken = default);

    Task<string> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default);
    DateTime GetRefreshTokenExpiryTime();
    DateTime GetAccessTokenExpiryTime();
}
