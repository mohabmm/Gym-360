namespace GymApp.Modules.Identity.Application.DTOs;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? GymId = null,
    string? GymName = null,
    string? PrincipalType = null,
    Guid? OwnerId = null,
    IReadOnlyList<OwnedGymSummary>? OwnedGyms = null,
    string? DashboardMode = null);

public sealed record OwnedGymSummary(
    Guid GymId,
    string Name,
    string Slug,
    string SchemaPrefix);

