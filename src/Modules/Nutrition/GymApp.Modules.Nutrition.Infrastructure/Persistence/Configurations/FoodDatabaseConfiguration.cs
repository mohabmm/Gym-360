using GymApp.Modules.Nutrition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class FoodDatabaseConfiguration : IEntityTypeConfiguration<FoodDatabase>
{
    public void Configure(EntityTypeBuilder<FoodDatabase> builder)
    {
        builder.ToTable("food_database");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.NameArabic)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ServingSize)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(e => e.ServingUnit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CaloriesPerServing)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(e => e.ProteinPerServing)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(e => e.CarbsPerServing)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(e => e.FatPerServing)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_food_database_category");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_food_database_name");

        builder.HasIndex(e => e.NameArabic)
            .HasDatabaseName("IX_food_database_name_arabic");
    }
}
