using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .ValueGeneratedNever();

        builder.Property(sp => sp.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(sp => sp.Code)
            .IsUnique();

        builder.Property(sp => sp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .HasMaxLength(500);

        builder.Property(sp => sp.MonthlyPriceEgp)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(sp => sp.AnnualPriceEgp)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(sp => sp.MemberLimit)
            .IsRequired();

        builder.Property(sp => sp.TrainerLimit)
            .IsRequired();

        builder.Property(sp => sp.StaffLimit)
            .IsRequired();

        builder.Property(sp => sp.IsActive)
            .IsRequired();

        builder.Property(sp => sp.IsPublic)
            .IsRequired();

        builder.Property(sp => sp.IsTimeLimited)
            .IsRequired();

        builder.Property(sp => sp.AvailableFrom);

        builder.Property(sp => sp.AvailableUntil);

        builder.Property(sp => sp.SortOrder)
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt);
    }
}
