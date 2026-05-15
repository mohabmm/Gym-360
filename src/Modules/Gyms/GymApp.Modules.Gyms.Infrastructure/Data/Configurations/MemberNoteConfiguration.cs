using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class MemberNoteConfiguration : IEntityTypeConfiguration<MemberNote>
{
    public void Configure(EntityTypeBuilder<MemberNote> builder)
    {
        builder.ToTable("member_notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.GymId).IsRequired();
        builder.Property(n => n.MemberId).IsRequired();
        builder.Property(n => n.CreatedBy).IsRequired();

        builder.Property(n => n.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => new { n.GymId, n.MemberId });
    }
}
