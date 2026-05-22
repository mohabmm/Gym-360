using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymSeasonalHoursConfiguration : IEntityTypeConfiguration<GymSeasonalHours>
{
    public void Configure(EntityTypeBuilder<GymSeasonalHours> builder)
    {
        builder.ToTable("gym_seasonal_hours");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.GymId).IsRequired();

        builder.Property(h => h.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.StartDate).IsRequired();
        builder.Property(h => h.EndDate).IsRequired();
        builder.Property(h => h.IsActive).IsRequired();

        builder.OwnsMany(h => h.DaySchedules, ds =>
        {
            ds.ToTable("gym_seasonal_day_schedules");
            ds.WithOwner().HasForeignKey("GymSeasonalHoursId");
            ds.Property(d => d.DayOfWeek).HasConversion<string>().HasMaxLength(20);
            ds.Property(d => d.OpenTime);
            ds.Property(d => d.CloseTime);
            ds.Property(d => d.IsClosed);
        });

        builder.HasIndex(h => h.GymId);
    }
}
