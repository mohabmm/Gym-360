using GymApp.Common.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    public string ModuleName => "Nutrition";

    public Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Nutrition data (food logs, water logs, meal plans) is scoped per gym.
        // Use the Nutrition endpoints with X-Gym-Id header to retrieve per-gym data.
        return Task.FromResult<object>(new
        {
            UserId = userId,
            Note = "Nutrition data is gym-scoped. Use the Nutrition endpoints with X-Gym-Id header to retrieve per-gym data.",
            DataCategories = new[] { "FoodLogs", "WaterLogs", "MealPlans" }
        });
    }
}
