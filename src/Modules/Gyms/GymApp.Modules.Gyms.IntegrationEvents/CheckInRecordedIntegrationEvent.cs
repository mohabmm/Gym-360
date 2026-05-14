using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record CheckInRecordedIntegrationEvent(Guid GymId, Guid MemberId, DateTime CheckedInAt) : IDomainEvent;
