using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features");

        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.Id)
            .ValueGeneratedNever();

        builder.Property(pf => pf.PlanId)
            .IsRequired();

        builder.Property(pf => pf.FeatureId)
            .IsRequired();

        builder.Property(pf => pf.IsEnabled)
            .IsRequired();

        builder.Property(pf => pf.LimitValue);

        builder.HasIndex(pf => new { pf.PlanId, pf.FeatureId })
            .IsUnique();
    }
}
