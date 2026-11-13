namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A member's packages in one gym.</summary>
public sealed record PackageOwner(Guid GymId, Guid MemberId);

/// <summary>The member and coach a PT package is sold for, in one gym.</summary>
public sealed record PackageParticipants(Guid GymId, Guid MemberId, Guid TrainerId);

/// <summary>A session that was counted against a package, in one gym.</summary>
public sealed record PackageSessionRef(Guid GymId, Guid SessionId);
