using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record ProgramCreatedDomainEvent(Guid ProgramId, Guid TrainerId) : IDomainEvent;
