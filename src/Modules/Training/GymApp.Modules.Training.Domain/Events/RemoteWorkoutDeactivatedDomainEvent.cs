using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record RemoteWorkoutDeactivatedDomainEvent(Guid TrainerId, Guid ClientId, Guid GymId) : IDomainEvent;
