using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class PlanTemplateConfiguration : IEntityTypeConfiguration<PlanTemplate>
{
    public void Configure(EntityTypeBuilder<PlanTemplate> builder)
    {
        builder.ToTable("plan_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Scope).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Goal).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Name).HasMaxLength(PlanDocumentRules.MaxNameLength).IsRequired();
        builder.Property(t => t.NameAr).HasMaxLength(PlanDocumentRules.MaxNameLength);
        builder.Property(t => t.GoalText).HasMaxLength(PlanDocumentRules.MaxNameLength);
        builder.MapDocument(nameof(PlanTemplate.Document));
        builder.Property(t => t.SampleKey).HasMaxLength(50);
        builder.Ignore(t => t.Content);
        builder.Ignore(t => t.IsSample);

        builder.HasIndex(t => new { t.GymId, t.SampleKey })
            .IsUnique()
            .HasFilter("[SampleKey] IS NOT NULL")
            .HasDatabaseName("UX_plan_templates_sample");

        builder.HasIndex(t => new { t.GymId, t.Scope, t.IsArchived });
        builder.HasIndex(t => new { t.GymId, t.OwnerTrainerId });
    }
}
