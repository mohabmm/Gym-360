using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymTrainerRequestConfiguration : IEntityTypeConfiguration<GymTrainerRequest>
{
    public void Configure(EntityTypeBuilder<GymTrainerRequest> builder)
    {
        builder.ToTable("gym_trainer_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.GymId).IsRequired();
        builder.Property(r => r.TrainerId).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.RequestedAt).IsRequired();
        builder.Property(r => r.DecidedAt);

        builder.Property(r => r.Bio)
            .HasMaxLength(2000);

        builder.PrimitiveCollection<List<string>>("_specializations")
            .HasColumnName("Specializations")
            .ElementType()
            .HasMaxLength(100);

        builder.HasIndex(r => new { r.GymId, r.TrainerId }).IsUnique();
        builder.HasIndex(r => new { r.GymId, r.Status });
    }
}
