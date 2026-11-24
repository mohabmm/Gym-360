using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class PlanProgramUnlockConfiguration : IEntityTypeConfiguration<PlanProgramUnlock>
{
    public void Configure(EntityTypeBuilder<PlanProgramUnlock> builder)
    {
        builder.ToTable("plan_program_unlocks");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        // One row per coach–member pair; locking again updates it instead of adding another.
        builder.HasIndex(u => new { u.GymId, u.TraineeId, u.TrainerId }).IsUnique();
        builder.Property(u => u.Length).HasConversion<string>().HasMaxLength(20).HasDefaultValue(UnlockLength.OneWeek);
        builder.Ignore(u => u.IsUnlocked);
    }
}
