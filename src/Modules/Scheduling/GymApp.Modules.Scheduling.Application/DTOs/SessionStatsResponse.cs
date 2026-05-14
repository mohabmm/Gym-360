namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record SessionStatsResponse(
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    int TotalScheduled,
    double CompletionRate);
