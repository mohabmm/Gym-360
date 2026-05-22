using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record GymApprovedIntegrationEvent(Guid GymId, Guid OwnerUserId) : IDomainEvent;
