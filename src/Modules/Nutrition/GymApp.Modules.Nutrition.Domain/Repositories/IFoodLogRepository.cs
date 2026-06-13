using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Domain.Repositories;

public interface IFoodLogRepository
{
    Task<FoodLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FoodLog?> GetByMealIdAsync(Guid mealEntryId, CancellationToken cancellationToken = default);
    Task<FoodLog?> GetByUserAndDateAsync(Guid userId, Guid gymId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<FoodLog>> GetByUserAsync(Guid userId, Guid gymId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task AddAsync(FoodLog foodLog, CancellationToken cancellationToken = default);
    void Update(FoodLog foodLog);
}
