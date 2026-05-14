using GymApp.Common.Domain;

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

    public static FoodDatabase Create(
        string name,
        string nameArabic,
        decimal servingSize,
        string servingUnit,
        decimal caloriesPerServing,
        decimal proteinPerServing,
        decimal carbsPerServing,
        decimal fatPerServing,
        string category)
    {
        return new FoodDatabase
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameArabic = nameArabic,
            ServingSize = servingSize,
            ServingUnit = servingUnit,
            CaloriesPerServing = caloriesPerServing,
            ProteinPerServing = proteinPerServing,
            CarbsPerServing = carbsPerServing,
            FatPerServing = fatPerServing,
            Category = category,
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
