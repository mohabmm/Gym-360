using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Domain.ValueObjects;

/// <summary>Who a notification is for (user, optional gym), what kind it is, and its metadata.</summary>
public sealed record NotificationDetails
{
    public required Guid UserId { get; init; }
    public required NotificationType Type { get; init; }
    public Guid? GymId { get; init; }
    public Dictionary<string, string>? Data { get; init; }
}
