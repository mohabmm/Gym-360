using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class MealEntryConfiguration : IEntityTypeConfiguration<MealEntry>
{
    public void Configure(EntityTypeBuilder<MealEntry> builder)
    {
        builder.ToTable("meal_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.FoodLogId)
            .IsRequired();

        builder.Property(e => e.MealType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.TotalCalories)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.TotalProtein)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.TotalCarbs)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.TotalFat)
            .HasPrecision(10, 2)
            .IsRequired();

        // Store FoodItems as a JSON column
        builder.OwnsMany(e => e.FoodItems, fi =>
        {
            fi.ToJson("food_items");
            fi.Property(f => f.Name).HasMaxLength(200);
            fi.Property(f => f.Quantity).HasPrecision(10, 2);
            fi.Property(f => f.Unit).HasMaxLength(50);
            fi.Property(f => f.Calories).HasPrecision(10, 2);
            fi.Property(f => f.Protein).HasPrecision(10, 2);
            fi.Property(f => f.Carbs).HasPrecision(10, 2);
            fi.Property(f => f.Fat).HasPrecision(10, 2);
        });

        builder.HasIndex(e => e.FoodLogId)
            .HasDatabaseName("IX_meal_entries_food_log");
    }
}
