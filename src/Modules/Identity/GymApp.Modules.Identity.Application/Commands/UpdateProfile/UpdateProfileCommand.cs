using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? ProfilePhotoUrl,
    string? DateOfBirth,
    string? Gender,
    string? Language) : ICommand;
