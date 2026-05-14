using GymApp.Common.Domain;

namespace GymApp.Common.Application.Errors;

/// <summary>
/// Cross-cutting errors raised when a caller's gym context is missing, invalid,
/// or blocked by the gym's state (suspension). Shared by trainee-facing /me
/// endpoints across modules.
/// </summary>
public static class GymAccessErrors
{
    public static readonly Error GymContextRequired = new(
        "GYM_CONTEXT_REQUIRED",
        "The X-Gym-Id header is required and must be a valid GUID.");

    public static readonly Error NotAMemberOfGym = new(
        "NOT_A_MEMBER_OF_GYM",
        "You are not an active member of this gym.");

    /// <summary>
    /// Used by <c>/me/*</c> endpoints that are scoped to the Member role.
    /// Trainers and staff are active in the gym but are not "members" in the
    /// billing/nutrition sense, so the old <see cref="NotAMemberOfGym"/> message
    /// read as false to them. This error distinguishes "wrong role" from
    /// "no relationship to this gym at all".
    /// </summary>
    public static readonly Error MemberRoleRequired = new(
        "MEMBER_ROLE_REQUIRED",
        "This endpoint is only available to gym members. Trainers and staff should use role-specific endpoints.");

    public static readonly Error GymSuspended = new(
        "GYM_SUSPENDED",
        "This gym is currently suspended.");
}
