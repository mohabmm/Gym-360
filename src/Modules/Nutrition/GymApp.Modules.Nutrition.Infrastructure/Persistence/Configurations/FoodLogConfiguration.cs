using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class FoodLogConfiguration : IEntityTypeConfiguration<FoodLog>
{
    public void Configure(EntityTypeBuilder<FoodLog> builder)
    {
        builder.ToTable("food_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.OwnsOne(e => e.DailyTotals, totals =>
        {
            totals.Property(t => t.TotalCalories).HasPrecision(10, 2).HasColumnName("total_calories");
            totals.Property(t => t.TotalProtein).HasPrecision(10, 2).HasColumnName("total_protein");
            totals.Property(t => t.TotalCarbs).HasPrecision(10, 2).HasColumnName("total_carbs");
            totals.Property(t => t.TotalFat).HasPrecision(10, 2).HasColumnName("total_fat");
        });

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasMany(e => e.Meals)
            .WithOne()
            .HasForeignKey(m => m.FoodLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.GymId, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_food_logs_user_gym_date");
    }
}
