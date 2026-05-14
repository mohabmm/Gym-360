namespace GymApp.Modules.Identity.Application.DTOs;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User);
