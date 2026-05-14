using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.IntegrationEvents;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IDomainEvent;
