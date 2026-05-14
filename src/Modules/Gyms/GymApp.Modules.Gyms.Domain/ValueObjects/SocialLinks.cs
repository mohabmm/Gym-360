namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record SocialLinks(
    string? Facebook = null,
    string? Instagram = null,
    string? Twitter = null,
    string? Website = null);
