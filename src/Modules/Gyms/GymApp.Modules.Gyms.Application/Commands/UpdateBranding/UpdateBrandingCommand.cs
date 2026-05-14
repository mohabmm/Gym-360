using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateBranding;

public sealed record UpdateBrandingCommand(
    Guid GymId,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CoverPhotoUrl) : ICommand;
