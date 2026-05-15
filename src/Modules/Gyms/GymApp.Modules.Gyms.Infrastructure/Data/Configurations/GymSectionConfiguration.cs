using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymSectionConfiguration : IEntityTypeConfiguration<GymSection>
{
    public void Configure(EntityTypeBuilder<GymSection> builder)
    {
        builder.ToTable("gym_sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.GymId).IsRequired();

        builder.Property(s => s.SectionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200);

        builder.Property(s => s.IsActive).IsRequired();

        builder.HasIndex(s => s.GymId);
    }
}
