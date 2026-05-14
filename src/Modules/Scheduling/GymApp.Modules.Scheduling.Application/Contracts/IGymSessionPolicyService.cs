namespace GymApp.Modules.Scheduling.Application.Contracts;

public interface IGymSessionPolicyService
{
    Task<GymSessionPolicyDto?> GetPolicyAsync(Guid gymId, CancellationToken cancellationToken = default);
}

public sealed record GymSessionPolicyDto(
    int CancellationWindowHours,
    int NoshowThreshold,
    int NoshowWindowDays,
    string NoshowAction);
