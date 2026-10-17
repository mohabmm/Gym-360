namespace GymApp.Modules.Progress.Domain.Repositories;

/// <summary>A 1-based page of results.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}
