using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries;

/// <summary>A normalized 1-based page request: page &gt;= 1 and page size clamped to [1, max].</summary>
internal sealed class PageWindow
{
    private const int MinPageSize = 1;

    private PageWindow(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }

    public static PageWindow Create(int page, int pageSize, int maxPageSize) =>
        new(page < 1 ? 1 : page, Math.Clamp(pageSize, MinPageSize, maxPageSize));

    public List<T> Slice<T>(IEnumerable<T> source) =>
        source
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    public PagedResponse<T> ToResponse<T>(List<T> items, int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / PageSize);
        return new PagedResponse<T>(items, Page, PageSize, totalCount, totalPages);
    }
}
