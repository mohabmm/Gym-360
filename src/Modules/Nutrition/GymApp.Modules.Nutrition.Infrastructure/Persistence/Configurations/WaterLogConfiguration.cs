using GymApp.Modules.Nutrition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Configurations;

internal sealed class WaterLogConfiguration : IEntityTypeConfiguration<WaterLog>
{
    public void Configure(EntityTypeBuilder<WaterLog> builder)
    {
        builder.ToTable("water_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.TotalMl)
            .IsRequired();

        builder.Property(e => e.TargetMl)
            .IsRequired()
            .HasDefaultValue(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasMany(e => e.Entries)
            .WithOne()
            .HasForeignKey(w => w.WaterLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_water_logs_user_date");

        builder.HasIndex(e => new { e.UserId, e.GymId })
            .HasDatabaseName("IX_water_logs_user_gym");
    }
}
