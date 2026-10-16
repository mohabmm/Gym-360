using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        ConfigureCredentials(builder);
        ConfigurePersonalDetails(builder);
        ConfigureClassification(builder);
        ConfigureAuditAndOwnership(builder);

        builder.OwnsMany(u => u.GymMemberships, ConfigureGymMemberships);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(u => u.GymMemberships);
    }

    private static void ConfigureCredentials(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PhoneNumber)
            .HasConversion(
                phone => phone.Value,
                value => PhoneNumber.Create(value).Value)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();
    }

    private static void ConfigurePersonalDetails(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.ProfilePhotoUrl)
            .HasColumnName("profile_photo_url")
            .HasMaxLength(500);

        builder.Property(u => u.DateOfBirth)
            .HasColumnName("date_of_birth");

        // National ID: non-unique on purpose — dirty/duplicate legacy source data
        // must not break a bulk migration. 14-char Egyptian NID, but allow a little
        // slack for non-Egyptian/passport edge cases.
        builder.Property(u => u.NationalId)
            .HasColumnName("national_id")
            .HasMaxLength(20);
    }

    private static void ConfigureClassification(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.Language)
            .HasColumnName("language")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
    }

    private static void ConfigureAuditAndOwnership(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(u => u.OwnerId)
            .HasColumnName("owner_id");

        builder.HasIndex(u => u.OwnerId);
    }

    private static void ConfigureGymMemberships(OwnedNavigationBuilder<User, GymMembership> membership)
    {
        membership.ToTable("user_gym_memberships");

        membership.WithOwner().HasForeignKey("UserId");

        membership.Property<int>("Id")
            .ValueGeneratedOnAdd();
        membership.HasKey("Id");

        membership.Property(m => m.GymId)
            .HasColumnName("gym_id")
            .IsRequired();

        membership.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        membership.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        membership.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        membership.Property(m => m.Section)
            .HasColumnName("section")
            .HasMaxLength(50);

        membership.Property(m => m.MemberNumber)
            .HasColumnName("member_number")
            .HasMaxLength(50);

        membership.HasIndex("UserId", nameof(GymMembership.GymId), nameof(GymMembership.Role));

        // One-Gym principal rule (FOUNDER_DECISIONS Q3 revised 2026-05-01):
        // each non-Owner principal (trainee/trainer/staff) has at most one
        // ACTIVE membership across all gyms. Owner principals scope across
        // many gyms via User.OwnerId, not via memberships.
        membership.HasIndex("UserId")
            .IsUnique()
            .HasFilter("[is_active] = 1")
            .HasDatabaseName("UX_user_gym_memberships_active_per_user");
    }
}
