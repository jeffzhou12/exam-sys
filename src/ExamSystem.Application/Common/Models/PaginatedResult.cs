namespace ExamSystem.Application.Common.Models;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public bool HasMore => Page * PageSize < TotalCount;

    public static PaginatedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}
