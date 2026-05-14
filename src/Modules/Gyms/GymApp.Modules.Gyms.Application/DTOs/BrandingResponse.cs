namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record BrandingResponse(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CoverPhotoUrl);
