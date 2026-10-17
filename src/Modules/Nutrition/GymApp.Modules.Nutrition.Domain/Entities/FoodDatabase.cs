using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class FoodDatabase : Entity
{
    private FoodDatabase() { }

    public string Name { get; private set; } = default!;
    public string NameArabic { get; private set; } = default!;
    public decimal ServingSize { get; private set; }
    public string ServingUnit { get; private set; } = default!;
    public decimal CaloriesPerServing { get; private set; }
    public decimal ProteinPerServing { get; private set; }
    public decimal CarbsPerServing { get; private set; }
    public decimal FatPerServing { get; private set; }
    public string Category { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public static FoodDatabase Create(FoodDatabaseDetails details)
    {
        return new FoodDatabase
        {
            Id = Guid.NewGuid(),
            Name = details.Name,
            NameArabic = details.NameArabic,
            ServingSize = details.ServingSize,
            ServingUnit = details.ServingUnit,
            CaloriesPerServing = details.CaloriesPerServing,
            ProteinPerServing = details.ProteinPerServing,
            CarbsPerServing = details.CarbsPerServing,
            FatPerServing = details.FatPerServing,
            Category = details.Category,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
