using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

/// <summary>The user-editable part of a <see cref="Entities.User"/>.</summary>
public sealed record UserProfile(
    string FirstName,
    string LastName,
    string? ProfilePhotoUrl,
    DateTime? DateOfBirth,
    Language Language);
