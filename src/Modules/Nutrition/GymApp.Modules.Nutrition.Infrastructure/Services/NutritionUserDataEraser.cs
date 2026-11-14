using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Services;

/// <summary>
/// Deletes the user's food / water logs (entries cascade) and meal plans assigned to them.
/// Meal plans a trainer wrote for other clients stay with those clients.
/// </summary>
internal sealed class NutritionUserDataEraser : IUserDataEraser
{
    private readonly NutritionDbContext _db;

    public NutritionUserDataEraser(NutritionDbContext db) => _db = db;

    public string ModuleName => "Nutrition";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.FoodLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.WaterLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.MealPlans.Where(p => p.ClientId == userId).ExecuteDeleteAsync(cancellationToken);
        return count;
    }
}
