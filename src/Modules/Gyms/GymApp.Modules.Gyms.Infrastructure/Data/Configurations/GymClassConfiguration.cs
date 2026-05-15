using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymClassConfiguration : IEntityTypeConfiguration<GymClass>
{
    public void Configure(EntityTypeBuilder<GymClass> builder)
    {
        builder.ToTable("classes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.GymId).IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.TrainerId).IsRequired();

        builder.OwnsOne(c => c.Schedule, schedule =>
        {
            schedule.Property(s => s.DayOfWeek)
                .HasColumnName("Schedule_DayOfWeek")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            schedule.Property(s => s.StartTime)
                .HasColumnName("Schedule_StartTime")
                .IsRequired();
            schedule.Property(s => s.DurationMinutes)
                .HasColumnName("Schedule_DurationMinutes")
                .IsRequired();
            schedule.Property(s => s.IsRecurring)
                .HasColumnName("Schedule_IsRecurring")
                .IsRequired();
        });

        builder.Property(c => c.Capacity).IsRequired();
        builder.Property(c => c.WaitlistEnabled).IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasMany(c => c.Bookings)
            .WithOne()
            .HasForeignKey(b => b.GymClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.GymId);
        builder.HasIndex(c => c.TrainerId);
    }
}
