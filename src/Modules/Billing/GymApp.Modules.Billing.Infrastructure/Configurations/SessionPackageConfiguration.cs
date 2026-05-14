using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class SessionPackageConfiguration : IEntityTypeConfiguration<SessionPackage>
{
    public void Configure(EntityTypeBuilder<SessionPackage> builder)
    {
        builder.ToTable("session_packages");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .ValueGeneratedNever();

        builder.Property(sp => sp.GymId)
            .IsRequired();

        builder.Property(sp => sp.TrainerId)
            .IsRequired();

        builder.Property(sp => sp.MemberId)
            .IsRequired();

        builder.Property(sp => sp.TemplateId)
            .IsRequired();

        builder.Property(sp => sp.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sp => sp.TotalSessions)
            .IsRequired();

        builder.Property(sp => sp.RemainingSessions)
            .IsRequired();

        builder.OwnsOne(sp => sp.PricePerSession, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("price_per_session_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("price_per_session_currency")
                .HasMaxLength(3)
                .HasDefaultValue("EGP")
                .IsRequired();
        });

        builder.OwnsOne(sp => sp.TotalPrice, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("total_price_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("total_price_currency")
                .HasMaxLength(3)
                .HasDefaultValue("EGP")
                .IsRequired();
        });

        builder.Property(sp => sp.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sp => sp.ExpiresAt);

        builder.Property(sp => sp.AssignedBy)
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.HasMany(sp => sp.UsageHistory)
            .WithOne()
            .HasForeignKey(pu => pu.SessionPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => sp.GymId);
        builder.HasIndex(sp => sp.MemberId);
        builder.HasIndex(sp => sp.TrainerId);
    }
}
