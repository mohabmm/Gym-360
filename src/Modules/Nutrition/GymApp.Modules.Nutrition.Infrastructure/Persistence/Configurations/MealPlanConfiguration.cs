using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("meal_plans");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.TrainerId)
            .IsRequired();

        builder.Property(e => e.ClientId);

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.DailyCalorieTarget)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.OwnsOne(e => e.MacroTargets, mt =>
        {
            mt.Property(m => m.ProteinGrams).HasPrecision(10, 2).HasColumnName("macro_protein_grams");
            mt.Property(m => m.CarbsGrams).HasPrecision(10, 2).HasColumnName("macro_carbs_grams");
            mt.Property(m => m.FatGrams).HasPrecision(10, 2).HasColumnName("macro_fat_grams");
        });

        builder.Property(e => e.IsTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasMany(e => e.Days)
            .WithOne()
            .HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TrainerId, e.GymId })
            .HasDatabaseName("IX_meal_plans_trainer_gym");

        builder.HasIndex(e => new { e.ClientId, e.GymId })
            .HasDatabaseName("IX_meal_plans_client_gym");
    }
}
