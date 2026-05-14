using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class SessionPackageTemplateConfiguration : IEntityTypeConfiguration<SessionPackageTemplate>
{
    public void Configure(EntityTypeBuilder<SessionPackageTemplate> builder)
    {
        builder.ToTable("session_package_templates");

        builder.HasKey(spt => spt.Id);

        builder.Property(spt => spt.Id)
            .ValueGeneratedNever();

        builder.Property(spt => spt.GymId)
            .IsRequired();

        builder.Property(spt => spt.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(spt => spt.SessionCount)
            .IsRequired();

        builder.OwnsOne(spt => spt.PricePerSession, priceBuilder =>
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

        builder.OwnsOne(spt => spt.TotalPrice, priceBuilder =>
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

        builder.Property(spt => spt.IsActive)
            .IsRequired();

        builder.Property(spt => spt.CreatedAt)
            .IsRequired();

        builder.HasIndex(spt => spt.GymId);
    }
}
