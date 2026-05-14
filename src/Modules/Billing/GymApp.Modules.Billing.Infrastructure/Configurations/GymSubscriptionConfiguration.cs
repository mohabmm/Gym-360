using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class GymSubscriptionConfiguration : IEntityTypeConfiguration<GymSubscription>
{
    public void Configure(EntityTypeBuilder<GymSubscription> builder)
    {
        builder.ToTable("gym_subscriptions");

        builder.HasKey(gs => gs.Id);

        builder.Property(gs => gs.Id)
            .ValueGeneratedNever();

        builder.Property(gs => gs.GymId)
            .IsRequired();

        builder.HasIndex(gs => gs.GymId)
            .IsUnique();

        builder.Property(gs => gs.PlanId)
            .IsRequired();

        builder.Property(gs => gs.StartedAt)
            .IsRequired();

        builder.Property(gs => gs.ExpiresAt);

        builder.Property(gs => gs.IsActive)
            .IsRequired();

        builder.Property(gs => gs.IsTrial)
            .IsRequired();

        builder.Property(gs => gs.TrialExpiresAt);

        builder.Property(gs => gs.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(gs => gs.LastPaymentAt);

        builder.Property(gs => gs.NextPaymentDue);

        builder.Property(gs => gs.OverdueSince);

        builder.Property(gs => gs.GracePeriodEndsAt);
    }
}
