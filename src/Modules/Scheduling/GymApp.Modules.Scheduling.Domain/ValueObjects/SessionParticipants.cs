namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

public sealed record SessionParticipants(Guid GymId, Guid TrainerId, Guid MemberId);
