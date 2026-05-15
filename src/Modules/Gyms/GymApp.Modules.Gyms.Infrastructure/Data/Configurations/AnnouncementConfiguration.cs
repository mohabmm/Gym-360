using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.GymId).IsRequired();

        builder.Property(a => a.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Content)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(a => a.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(a => a.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetAudience)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.PublishedAt);
        builder.Property(a => a.ScheduledAt);
        builder.Property(a => a.CreatedBy).IsRequired();

        builder.HasIndex(a => a.GymId);
    }
}
