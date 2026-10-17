namespace GymApp.Common.Application.Tenancy;

/// <summary>What is needed to provision a per-gym tenant: identity, owner, display name and slug.</summary>
/// <param name="GymCode">Optional join code; normalized to upper case when stored.</param>
public sealed record GymProvisioningRequest(
    Guid GymId,
    Guid OwnerId,
    string Name,
    string Slug,
    string? GymCode = null);
