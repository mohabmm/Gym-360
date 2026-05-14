using GymApp.Common.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    public string ModuleName => "Gamification";

    public Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Gamification data (trainer ratings, leaderboard entries, public profiles) is gym-scoped.
        // Use the Gamification endpoints with X-Gym-Id header to retrieve per-gym data.
        return Task.FromResult<object>(new
        {
            UserId = userId,
            Note = "Gamification data is gym-scoped. Use the Gamification endpoints with X-Gym-Id header to retrieve per-gym data.",
            DataCategories = new[] { "TrainerRatings", "LeaderboardEntries", "TrainerPublicProfile", "LeaderboardOptOut" }
        });
    }
}
