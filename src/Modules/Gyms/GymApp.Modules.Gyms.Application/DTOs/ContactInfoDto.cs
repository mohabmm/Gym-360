namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record ContactInfoDto(
    string Email,
    string Phone,
    string? Website = null);
