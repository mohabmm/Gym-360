namespace GymApp.Modules.Gyms.Application.DTOs;

/// <summary>Page and filters for a gym's member roster.</summary>
/// <param name="RestrictToMemberIds">
/// When non-null, only these members are eligible (applied BEFORE paging so
/// TotalCount/TotalPages stay correct). Used for the trainer-scoped roster.
/// </param>
public sealed record MemberRosterRequest(
    int Page,
    int PageSize,
    string? Status = null,
    string? Section = null,
    string? Search = null,
    IReadOnlyCollection<Guid>? RestrictToMemberIds = null);
