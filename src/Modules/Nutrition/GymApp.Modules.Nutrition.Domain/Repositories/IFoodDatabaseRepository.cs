using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Domain.Repositories;

public interface IFoodDatabaseRepository
{
    Task<List<FoodDatabase>> GetAllAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<FoodDatabase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FoodDatabase>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
