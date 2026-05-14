using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Repositories;

internal sealed class MealPlanRepository : IMealPlanRepository
{
    private readonly NutritionDbContext _dbContext;

    public MealPlanRepository(NutritionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .Include(m => m.Days)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<MealPlan>> GetByTrainerAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .AsNoTracking()
            .Include(m => m.Days)
            .Where(m => m.TrainerId == trainerId && m.GymId == gymId)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MealPlan>> GetByClientAsync(
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .AsNoTracking()
            .Include(m => m.Days)
            .Where(m => m.ClientId == clientId && m.GymId == gymId)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MealPlan>> GetTemplatesAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MealPlans
            .AsNoTracking()
            .Include(m => m.Days)
            .Where(m => m.IsTemplate && m.GymId == gymId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
    {
        await _dbContext.MealPlans.AddAsync(mealPlan, cancellationToken);
    }

    public void Update(MealPlan mealPlan)
    {
        _dbContext.MealPlans.Update(mealPlan);
    }
}
