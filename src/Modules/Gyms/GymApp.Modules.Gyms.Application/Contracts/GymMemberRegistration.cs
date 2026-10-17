namespace GymApp.Modules.Gyms.Application.Contracts;

/// <summary>A request to register a member in a gym's member directory.</summary>
/// <param name="PreferredNumber">Optional caller-supplied number. Must be unique within the gym.</param>
public sealed record GymMemberRegistration(
    Guid GymId,
    Guid MemberId,
    string? PreferredNumber,
    string? Section);
