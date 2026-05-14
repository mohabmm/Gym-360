using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record ExerciseCreatedDomainEvent(Guid ExerciseId) : IDomainEvent;
