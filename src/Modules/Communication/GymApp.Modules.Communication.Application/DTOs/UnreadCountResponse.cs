namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record UnreadCountResponse(
    int UnreadNotifications,
    int UnreadMessages);
