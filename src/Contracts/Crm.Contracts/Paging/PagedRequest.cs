namespace Crm.Contracts.Paging
{
    public sealed record PagedRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 50;
        public string? SortBy { get; init; }
        public string? SortDir { get; init; }
        public string? Search { get; init; }
    }
}
