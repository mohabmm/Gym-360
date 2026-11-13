namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>A trainee's answer to a coach-logged session: who answered, when, and (for "not me") why.</summary>
public sealed record MemberResponse(Guid MemberId, DateTime AtUtc, string? Reason = null);
