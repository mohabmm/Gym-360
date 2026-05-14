using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Configurations;

internal sealed class ProgressPhotoConfiguration : IEntityTypeConfiguration<ProgressPhoto>
{
    public void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        builder.ToTable("progress_photos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.PhotoUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.ThumbnailUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Angle)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.IsSharedWithTrainer)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.GymId })
            .HasDatabaseName("IX_progress_photos_user_gym");

        builder.HasIndex(e => new { e.UserId, e.Date })
            .HasDatabaseName("IX_progress_photos_user_date");
    }
}
