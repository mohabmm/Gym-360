using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class TrainerRateConfiguration : IEntityTypeConfiguration<TrainerRate>
{
    public void Configure(EntityTypeBuilder<TrainerRate> builder)
    {
        builder.ToTable("trainer_rates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.GymId).IsRequired();
        builder.Property(r => r.TrainerId).IsRequired();

        builder.Property(r => r.RatePerSession)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(r => r.EffectiveFrom).IsRequired();
        builder.Property(r => r.SetBy).IsRequired();

        builder.HasIndex(r => new { r.GymId, r.TrainerId })
            .IsUnique();
    }
}
