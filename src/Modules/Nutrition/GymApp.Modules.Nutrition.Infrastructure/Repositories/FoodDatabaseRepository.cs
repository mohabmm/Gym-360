using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Repositories;

internal sealed class FoodDatabaseRepository : IFoodDatabaseRepository
{
    private readonly NutritionDbContext _dbContext;

    public FoodDatabaseRepository(NutritionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<FoodDatabase>> GetAllAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FoodDatabaseItems
            .AsNoTracking()
            .Where(f => f.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category == category);
        }

        return await query
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FoodDatabase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FoodDatabaseItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive, cancellationToken);
    }

    public async Task<List<FoodDatabase>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.FoodDatabaseItems
            .AsNoTracking()
            .Where(f => f.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(f => f.Name.Contains(query) || f.NameArabic.Contains(query));
        }

        return await q
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }
}
