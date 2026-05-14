using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.ToTable("membership_plans");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.Id)
            .ValueGeneratedNever();

        builder.Property(mp => mp.GymId)
            .IsRequired();

        builder.Property(mp => mp.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(mp => mp.DurationDays)
            .IsRequired();

        builder.OwnsOne(mp => mp.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .HasDefaultValue("EGP")
                .IsRequired();
        });

        builder.Property(mp => mp.Features)
            .HasColumnName("features")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2),
                l => l.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                l => (IReadOnlyList<string>)l.ToList()));

        builder.Property(mp => mp.IsActive)
            .IsRequired();

        builder.Property(mp => mp.SortOrder)
            .IsRequired();

        builder.Property(mp => mp.CreatedAt)
            .IsRequired();

        builder.HasIndex(mp => mp.GymId);
    }
}
