using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.GymId)
            .IsRequired();

        builder.Property(p => p.MemberId)
            .IsRequired();

        builder.OwnsOne(p => p.Amount, amountBuilder =>
        {
            amountBuilder.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            amountBuilder.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("EGP")
                .IsRequired();
        });

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Purpose)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.ReferenceId);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ReceiptNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.ReceiptNumber)
            .IsUnique();

        builder.Property(p => p.RecordedBy)
            .IsRequired();

        builder.Property(p => p.VoidReason)
            .HasMaxLength(500);

        builder.Property(p => p.PaidAt);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.GymId);
        builder.HasIndex(p => p.MemberId);
    }
}
