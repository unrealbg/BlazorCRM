namespace Crm.Application.Dashboard
{
    public sealed record WonByMonthItem(int Year, int Month, decimal Sum);

    public sealed record ConversionItem(int Won, int Lost, double Rate);

    public sealed record OwnerSumItem(Guid? OwnerId, decimal Sum);

    public interface IDashboardService
    {
        Task<IReadOnlyList<WonByMonthItem>> GetWonByMonthAsync(int months, CancellationToken ct = default);

        Task<ConversionItem> GetConversionAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

        Task<IReadOnlyList<OwnerSumItem>> GetTopOwnersAsync(Guid? pipelineId, int take = 5, CancellationToken ct = default);
    }
}
