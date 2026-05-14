using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record MemberLoggedSessionDomainEvent(
    Guid SessionId,
    Guid MemberId) : IDomainEvent;
