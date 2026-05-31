using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Repositories;

internal sealed class FoodLogRepository : IFoodLogRepository
{
    private readonly NutritionDbContext _dbContext;

    public FoodLogRepository(NutritionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FoodLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FoodLogs
            .Include(f => f.Meals)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FoodLog?> GetByMealIdAsync(Guid mealEntryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FoodLogs
            .Include(f => f.Meals)
            .FirstOrDefaultAsync(f => f.Meals.Any(m => m.Id == mealEntryId), cancellationToken);
    }

    public async Task<FoodLog?> GetByUserAndDateAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.FoodLogs
            .Include(f => f.Meals)
            .FirstOrDefaultAsync(
                f => f.UserId == userId && f.GymId == gymId && f.Date == date,
                cancellationToken);
    }

    public async Task<List<FoodLog>> GetByUserAsync(
        Guid userId,
        Guid gymId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.FoodLogs
            .AsNoTracking()
            .Include(f => f.Meals)
            .Where(f => f.UserId == userId && f.GymId == gymId && f.Date >= fromDate && f.Date <= toDate)
            .OrderByDescending(f => f.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FoodLog foodLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.FoodLogs.AddAsync(foodLog, cancellationToken);
    }

    public void Update(FoodLog foodLog)
    {
        _dbContext.FoodLogs.Update(foodLog);
    }
}
