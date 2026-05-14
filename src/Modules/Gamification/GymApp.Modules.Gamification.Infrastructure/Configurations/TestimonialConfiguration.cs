using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.ToTable("testimonials");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.ProfileId)
            .IsRequired();

        builder.Property(t => t.CommentText)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(t => t.IsApproved)
            .IsRequired();

        builder.Property(t => t.ModeratedBy);

        builder.Property(t => t.ModeratedAt);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.ProfileId);
    }
}
