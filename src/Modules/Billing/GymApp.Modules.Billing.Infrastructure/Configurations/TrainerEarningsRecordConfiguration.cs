using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class TrainerEarningsRecordConfiguration : IEntityTypeConfiguration<TrainerEarningsRecord>
{
    public void Configure(EntityTypeBuilder<TrainerEarningsRecord> builder)
    {
        builder.ToTable("trainer_earnings_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.GymId).IsRequired();
        builder.Property(r => r.TrainerId).IsRequired();
        builder.Property(r => r.SessionId).IsRequired();
        builder.Property(r => r.MemberId).IsRequired();

        builder.Property(r => r.RateApplied)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(r => r.EarnedAt).IsRequired();

        builder.HasIndex(r => new { r.GymId, r.TrainerId, r.EarnedAt });
    }
}
