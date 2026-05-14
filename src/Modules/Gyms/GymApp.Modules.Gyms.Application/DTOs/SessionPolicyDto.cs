namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record SessionPolicyDto(
    Guid GymId,
    int CancellationWindowHours,
    int NoshowThreshold,
    int NoshowWindowDays,
    string NoshowAction);
