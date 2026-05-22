using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymConfiguration : IEntityTypeConfiguration<Gym>
{
    public void Configure(EntityTypeBuilder<Gym> builder)
    {
        builder.ToTable("gyms");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.OwnerId)
            .IsRequired();

        builder.Property(g => g.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.Description)
            .HasMaxLength(2000);

        builder.OwnsOne(g => g.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Address_Street").HasMaxLength(500).IsRequired();
            address.Property(a => a.City).HasColumnName("Address_City").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("Address_State").HasMaxLength(100).IsRequired();
            address.Property(a => a.Country).HasColumnName("Address_Country").HasMaxLength(100).IsRequired();
            address.Property(a => a.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(20).IsRequired();
            address.Property(a => a.Latitude).HasColumnName("Address_Latitude");
            address.Property(a => a.Longitude).HasColumnName("Address_Longitude");
        });

        builder.OwnsOne(g => g.ContactInfo, contact =>
        {
            contact.Property(c => c.Email).HasColumnName("Contact_Email").HasMaxLength(256).IsRequired();
            contact.Property(c => c.Phone).HasColumnName("Contact_Phone").HasMaxLength(50).IsRequired();
            contact.Property(c => c.Website).HasColumnName("Contact_Website").HasMaxLength(500);
        });

        builder.OwnsOne(g => g.Branding, branding =>
        {
            branding.Property(b => b.LogoUrl).HasColumnName("Branding_LogoUrl").HasMaxLength(1000);
            branding.Property(b => b.PrimaryColor).HasColumnName("Branding_PrimaryColor").HasMaxLength(7);
            branding.Property(b => b.SecondaryColor).HasColumnName("Branding_SecondaryColor").HasMaxLength(7);
            branding.Property(b => b.CoverPhotoUrl).HasColumnName("Branding_CoverPhotoUrl").HasMaxLength(1000);
        });

        builder.OwnsOne(g => g.GymCode, gymCode =>
        {
            gymCode.Property(c => c.Value)
                .HasColumnName("GymCode")
                .HasMaxLength(6)
                .IsRequired();

            gymCode.HasIndex(c => c.Value)
                .IsUnique();
        });

        builder.OwnsOne(g => g.SocialLinks, social =>
        {
            social.Property(s => s.Facebook).HasColumnName("Social_Facebook").HasMaxLength(500);
            social.Property(s => s.Instagram).HasColumnName("Social_Instagram").HasMaxLength(500);
            social.Property(s => s.Twitter).HasColumnName("Social_Twitter").HasMaxLength(500);
            social.Property(s => s.Website).HasColumnName("Social_Website").HasMaxLength(500);
        });

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(g => g.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsMany(g => g.OperatingHours, oh =>
        {
            oh.ToTable("gym_operating_hours");
            oh.WithOwner().HasForeignKey("GymId");
            oh.Property(d => d.DayOfWeek).HasConversion<string>().HasMaxLength(20);
            oh.Property(d => d.OpenTime);
            oh.Property(d => d.CloseTime);
            oh.Property(d => d.IsClosed);
        });

        builder.PrimitiveCollection<List<string>>("_amenities")
            .HasColumnName("Amenities")
            .ElementType()
            .HasMaxLength(100);

        // IReadOnlyList cannot be used directly with PrimitiveCollection — configure via backing field.
        builder.PrimitiveCollection<List<Guid>>("_memberIds")
            .HasColumnName("MemberIds");

        builder.PrimitiveCollection<List<Guid>>("_trainerIds")
            .HasColumnName("TrainerIds");

        builder.Property(g => g.SuspendedAt);
        builder.Property(g => g.SuspensionReason).HasMaxLength(1000);
        builder.Property(g => g.GracePeriodEndsAt);
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();
        builder.Property(g => g.KioskPinHash)
            .HasColumnName("KioskPinHash")
            .HasMaxLength(120);

        builder.HasIndex(g => g.OwnerId);
    }
}
