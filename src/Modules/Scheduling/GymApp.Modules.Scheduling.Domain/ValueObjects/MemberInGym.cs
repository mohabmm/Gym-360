namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>A member's sessions in one gym.</summary>
public sealed record MemberInGym(Guid GymId, Guid MemberId);
