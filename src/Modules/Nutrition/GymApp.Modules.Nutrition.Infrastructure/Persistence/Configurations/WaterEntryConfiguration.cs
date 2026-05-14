using GymApp.Modules.Nutrition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class WaterEntryConfiguration : IEntityTypeConfiguration<WaterEntry>
{
    public void Configure(EntityTypeBuilder<WaterEntry> builder)
    {
        builder.ToTable("water_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.WaterLogId)
            .IsRequired();

        builder.Property(e => e.AmountMl)
            .IsRequired();

        builder.Property(e => e.LoggedAt)
            .IsRequired();

        builder.HasIndex(e => e.WaterLogId)
            .HasDatabaseName("IX_water_entries_water_log");
    }
}
