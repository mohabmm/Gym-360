using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Contracts;

public interface IIdentityService
{
    Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
