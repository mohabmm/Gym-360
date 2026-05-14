using GymApp.Common.Application.Abstractions;

namespace GymApp.Modules.Progress.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    public string ModuleName => "Progress";

    public Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Progress data (body metrics, progress photos, streaks) is scoped per gym.
        // A full cross-gym export requires iterating over all gyms the user belongs to,
        // which is tracked in the Identity module. Data is available per gym via the
        // GET /api/v1/progress endpoints with the X-Gym-Id header.
        return Task.FromResult<object>(new
        {
            UserId = userId,
            Note = "Progress data is gym-scoped. Use the Progress endpoints with X-Gym-Id header to retrieve per-gym data.",
            DataCategories = new[] { "BodyMetrics", "ProgressPhotos", "Streaks" }
        });
    }
}
