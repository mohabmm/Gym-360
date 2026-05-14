using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Configurations;

internal sealed class BodyMetricEntryConfiguration : IEntityTypeConfiguration<BodyMetricEntry>
{
    public void Configure(EntityTypeBuilder<BodyMetricEntry> builder)
    {
        builder.ToTable("body_metric_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasPrecision(8, 2);

        builder.Property(e => e.WeightUnit)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.BodyFatPercentage)
            .HasPrecision(5, 2);

        builder.Property(e => e.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.LoggedBy)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.OwnsOne(e => e.Measurements, measurements =>
        {
            measurements.Property(m => m.Chest).HasPrecision(6, 2).HasColumnName("measurement_chest");
            measurements.Property(m => m.Waist).HasPrecision(6, 2).HasColumnName("measurement_waist");
            measurements.Property(m => m.Hips).HasPrecision(6, 2).HasColumnName("measurement_hips");
            measurements.Property(m => m.LeftArm).HasPrecision(6, 2).HasColumnName("measurement_left_arm");
            measurements.Property(m => m.RightArm).HasPrecision(6, 2).HasColumnName("measurement_right_arm");
            measurements.Property(m => m.LeftThigh).HasPrecision(6, 2).HasColumnName("measurement_left_thigh");
            measurements.Property(m => m.RightThigh).HasPrecision(6, 2).HasColumnName("measurement_right_thigh");
            measurements.Property(m => m.LeftCalf).HasPrecision(6, 2).HasColumnName("measurement_left_calf");
            measurements.Property(m => m.RightCalf).HasPrecision(6, 2).HasColumnName("measurement_right_calf");
            measurements.Property(m => m.Neck).HasPrecision(6, 2).HasColumnName("measurement_neck");
            measurements.Property(m => m.MeasurementUnit)
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasColumnName("measurement_unit");
        });

        builder.HasIndex(e => new { e.UserId, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_body_metric_entries_user_date");

        builder.HasIndex(e => new { e.UserId, e.GymId })
            .HasDatabaseName("IX_body_metric_entries_user_gym");
    }
}
