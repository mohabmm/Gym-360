using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class WorkoutAssignmentConfiguration : IEntityTypeConfiguration<WorkoutAssignment>
{
    public void Configure(EntityTypeBuilder<WorkoutAssignment> builder)
    {
        builder.ToTable("workout_assignments");

        builder.HasKey(wa => wa.Id);

        builder.Property(wa => wa.Id)
            .ValueGeneratedNever();

        builder.Property(wa => wa.AssignmentType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(wa => wa.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(wa => wa.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(wa => new { wa.TrainerId, wa.ClientId, wa.GymId });
        builder.HasIndex(wa => new { wa.ClientId, wa.GymId, wa.Status });
    }
}
