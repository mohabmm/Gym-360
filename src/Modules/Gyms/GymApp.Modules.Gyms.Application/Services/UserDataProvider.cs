using GymApp.Common.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    public string ModuleName => "Gyms";

    public Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Gym membership data is stored in the Identity module (GymMembership value object on User).
        // The Gyms module manages gym-level entities (Gym, GymClass, Announcement) not per-user membership records.
        // Check-in history is queryable by gym, not by member across all gyms.
        return Task.FromResult<object>(new
        {
            Note = "Gym-level check-in records are accessible per gym. Membership linkage is tracked in the Identity module."
        });
    }
}
