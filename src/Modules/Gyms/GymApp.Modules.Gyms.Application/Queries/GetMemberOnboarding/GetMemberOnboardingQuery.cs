using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Queries.GetMemberOnboarding;

public sealed record MemberOnboardingDto(
    Guid Id,
    Guid GymId,
    Guid MemberId,
    string? MedicalHistory,
    string? FitnessGoals,
    string? Injuries,
    string? Medications,
    string? FitnessLevel);

/// <summary>
/// The member's saved intake form, or <c>null</c> when none has been filled in yet.
/// </summary>
public sealed record GetMemberOnboardingQuery(
    Guid GymId,
    Guid MemberId) : IQuery<MemberOnboardingDto?>;
