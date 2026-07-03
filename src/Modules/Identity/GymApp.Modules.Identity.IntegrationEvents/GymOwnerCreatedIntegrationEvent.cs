using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.IntegrationEvents;

/// <summary>
/// Raised when an admin creates a standalone GymOwner account (no gym yet).
/// The Gyms module consumes this to auto-provision the owner's first gym, so
/// an owner never logs in to a "No owned gyms found" dead-end.
/// </summary>
public sealed record GymOwnerCreatedIntegrationEvent(
    Guid OwnerUserId,
    string CompanyName,
    string BillingEmail,
    string? Phone) : IIntegrationEvent;
