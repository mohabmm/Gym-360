using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("user_consents");

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.Id)
            .ValueGeneratedNever();

        builder.Property(uc => uc.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(uc => uc.ConsentType)
            .HasColumnName("consent_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(uc => uc.IsGranted)
            .HasColumnName("is_granted")
            .IsRequired();

        builder.Property(uc => uc.GrantedAt)
            .HasColumnName("granted_at")
            .IsRequired();

        builder.Property(uc => uc.WithdrawnAt)
            .HasColumnName("withdrawn_at");

        builder.Property(uc => uc.ConsentText)
            .HasColumnName("consent_text")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(uc => uc.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(uc => new { uc.UserId, uc.ConsentType });
    }
}
