namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record PagedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
