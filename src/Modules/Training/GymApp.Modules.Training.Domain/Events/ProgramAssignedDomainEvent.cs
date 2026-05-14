using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record ProgramAssignedDomainEvent(Guid AssignmentId, Guid TrainerId, Guid ClientId) : IDomainEvent;
