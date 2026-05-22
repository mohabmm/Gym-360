using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class DataImportJobConfiguration : IEntityTypeConfiguration<DataImportJob>
{
    public void Configure(EntityTypeBuilder<DataImportJob> builder)
    {
        builder.ToTable("data_import_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.GymId).IsRequired();
        builder.Property(j => j.UploadedByUserId).IsRequired();
        builder.Property(j => j.Source).HasConversion<int>().IsRequired();
        builder.Property(j => j.Status).HasConversion<int>().IsRequired();

        builder.Property(j => j.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(j => j.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(j => j.FileSizeBytes).IsRequired();

        builder.Property(j => j.TotalRowCount).IsRequired();
        builder.Property(j => j.SuccessCount).IsRequired();
        builder.Property(j => j.FailureCount).IsRequired();

        builder.Property(j => j.ErrorReportPath).HasMaxLength(500);
        builder.Property(j => j.FailureReason).HasMaxLength(1000);

        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.StartedAt);
        builder.Property(j => j.CompletedAt);

        builder.HasIndex(j => new { j.GymId, j.CreatedAt });
        builder.HasIndex(j => j.Status);
    }
}
