using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record MemberAddedToGymIntegrationEvent(
    Guid GymId,
    Guid MemberId,
    string? Section,
    string? MemberNumber) : IDomainEvent;
