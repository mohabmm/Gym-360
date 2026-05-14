using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Communication.Infrastructure.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(np => np.Id);

        builder.Property(np => np.Id)
            .ValueGeneratedNever();

        builder.Property(np => np.UserId)
            .IsRequired();

        builder.Property(np => np.GymId);

        builder.Property(np => np.Preferences)
            .HasColumnName("preferences")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<NotificationTypePreference>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<NotificationTypePreference>())
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<NotificationTypePreference>>(
                (l1, l2) => l1 != null && l2 != null && l1.Count == l2.Count &&
                            l1.All(p1 => l2.Any(p2 => p2.NotificationType == p1.NotificationType &&
                                                       p2.PushEnabled == p1.PushEnabled &&
                                                       p2.InAppEnabled == p1.InAppEnabled)),
                l => l.Count,
                l => (IReadOnlyList<NotificationTypePreference>)l.ToList()));

        builder.Property(np => np.QuietHoursStart);

        builder.Property(np => np.QuietHoursEnd);

        builder.Property(np => np.UpdatedAt)
            .IsRequired();

        builder.HasIndex(np => new { np.UserId, np.GymId })
            .IsUnique();
    }
}
