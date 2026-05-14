using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Domain.Repositories;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MealPlan>> GetByTrainerAsync(Guid trainerId, Guid gymId, CancellationToken cancellationToken = default);
    Task<List<MealPlan>> GetByClientAsync(Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
    Task<List<MealPlan>> GetTemplatesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default);
    void Update(MealPlan mealPlan);
}
