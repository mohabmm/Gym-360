namespace GymApp.Modules.Identity.Application.DTOs;

public sealed record GymMembershipDto(
    Guid GymId,
    string Role,
    DateTime JoinedAt,
    bool IsActive,
    string? Section,
    string? MemberNumber,
    Guid? AssignedTrainerId,
    string? AssignedTrainerName);
