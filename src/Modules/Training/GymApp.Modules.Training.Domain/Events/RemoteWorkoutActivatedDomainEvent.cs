using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record RemoteWorkoutActivatedDomainEvent(Guid TrainerId, Guid ClientId, Guid GymId) : IDomainEvent;
