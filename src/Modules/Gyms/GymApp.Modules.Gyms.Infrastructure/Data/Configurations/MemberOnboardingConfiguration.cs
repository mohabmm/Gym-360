using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class MemberOnboardingConfiguration : IEntityTypeConfiguration<MemberOnboarding>
{
    public void Configure(EntityTypeBuilder<MemberOnboarding> builder)
    {
        builder.ToTable("member_onboardings");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.GymId).IsRequired();
        builder.Property(o => o.MemberId).IsRequired();

        builder.Property(o => o.MedicalHistory).HasMaxLength(2000);
        builder.Property(o => o.FitnessGoals).HasMaxLength(2000);
        builder.Property(o => o.Injuries).HasMaxLength(2000);
        builder.Property(o => o.Medications).HasMaxLength(2000);
        builder.Property(o => o.FitnessLevel).HasMaxLength(50);

        builder.HasIndex(o => new { o.GymId, o.MemberId }).IsUnique();
    }
}
