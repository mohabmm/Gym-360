using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record GymBrandingUpdatedIntegrationEvent(
    Guid GymId,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CoverPhotoUrl) : IIntegrationEvent;
