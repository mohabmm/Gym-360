using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record CheckInRecordedDomainEvent(Guid GymId, Guid MemberId, string Method) : IDomainEvent;
