namespace GymApp.Modules.Communication.Domain.ValueObjects;

/// <summary>A 1-based page number and page size for paged repository reads.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}
