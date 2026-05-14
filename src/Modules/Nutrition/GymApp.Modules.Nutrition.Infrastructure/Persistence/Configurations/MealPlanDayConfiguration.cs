using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay>
{
    public void Configure(EntityTypeBuilder<MealPlanDay> builder)
    {
        builder.ToTable("meal_plan_days");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.MealPlanId)
            .IsRequired();

        builder.Property(e => e.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        // Store PlannedMeals as JSON column
        builder.OwnsMany(e => e.PlannedMeals, pm =>
        {
            pm.ToJson("planned_meals");
            pm.Property(p => p.MealType).HasConversion<string>();
            pm.Property(p => p.Name).HasMaxLength(200);
            pm.Property(p => p.Description).HasMaxLength(500);
            pm.Property(p => p.TotalCalories).HasPrecision(10, 2);
            pm.Property(p => p.TotalProtein).HasPrecision(10, 2);
            pm.Property(p => p.TotalCarbs).HasPrecision(10, 2);
            pm.Property(p => p.TotalFat).HasPrecision(10, 2);
            pm.OwnsMany(p => p.FoodItems, fi =>
            {
                fi.Property(f => f.Name).HasMaxLength(200);
                fi.Property(f => f.Quantity).HasPrecision(10, 2);
                fi.Property(f => f.Unit).HasMaxLength(50);
                fi.Property(f => f.Calories).HasPrecision(10, 2);
                fi.Property(f => f.Protein).HasPrecision(10, 2);
                fi.Property(f => f.Carbs).HasPrecision(10, 2);
                fi.Property(f => f.Fat).HasPrecision(10, 2);
            });
        });

        // Store Alternatives as JSON column
        builder.OwnsMany(e => e.Alternatives, alt =>
        {
            alt.ToJson("alternatives");
            alt.Property(p => p.MealType).HasConversion<string>();
            alt.Property(p => p.Name).HasMaxLength(200);
            alt.Property(p => p.Description).HasMaxLength(500);
            alt.Property(p => p.TotalCalories).HasPrecision(10, 2);
            alt.Property(p => p.TotalProtein).HasPrecision(10, 2);
            alt.Property(p => p.TotalCarbs).HasPrecision(10, 2);
            alt.Property(p => p.TotalFat).HasPrecision(10, 2);
            alt.OwnsMany(p => p.FoodItems, fi =>
            {
                fi.Property(f => f.Name).HasMaxLength(200);
                fi.Property(f => f.Quantity).HasPrecision(10, 2);
                fi.Property(f => f.Unit).HasMaxLength(50);
                fi.Property(f => f.Calories).HasPrecision(10, 2);
                fi.Property(f => f.Protein).HasPrecision(10, 2);
                fi.Property(f => f.Carbs).HasPrecision(10, 2);
                fi.Property(f => f.Fat).HasPrecision(10, 2);
            });
        });

        builder.HasIndex(e => e.MealPlanId)
            .HasDatabaseName("IX_meal_plan_days_meal_plan");
    }
}
