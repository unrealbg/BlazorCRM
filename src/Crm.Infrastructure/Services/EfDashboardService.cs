namespace Crm.Infrastructure.Services;

using Crm.Application.Dashboard;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public sealed class EfDashboardService : IDashboardService
{
    private readonly CrmDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly Crm.Application.Common.Multitenancy.ITenantProvider _tenant;
    public EfDashboardService(CrmDbContext db, IMemoryCache cache, Crm.Application.Common.Multitenancy.ITenantProvider tenant)
    { _db = db; _cache = cache; _tenant = tenant; }

    private string Key(string name, params object[] parts) => $"{_tenant.TenantId:N}:{name}:{string.Join(':', parts)}";

    public async Task<IReadOnlyList<WonByMonthItem>> GetWonByMonthAsync(int months, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(Key("won", months), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            var since = DateTime.UtcNow.AddMonths(-months);
            return await _db.Deals.AsNoTracking()
                .Where(d => d.Status == Crm.Domain.Enums.DealStatus.Won && d.CloseDate >= since)
                .GroupBy(d => new { d.CloseDate!.Value.Year, d.CloseDate!.Value.Month })
                .Select(g => new WonByMonthItem(g.Key.Year, g.Key.Month, g.Sum(x => x.Amount)))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(ct);
        }) ?? new List<WonByMonthItem>();
    }

    public async Task<ConversionItem> GetConversionAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(Key("conv", fromUtc, toUtc), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            var won = await _db.Deals.AsNoTracking().CountAsync(d => d.Status == Crm.Domain.Enums.DealStatus.Won && d.CloseDate >= fromUtc && d.CloseDate <= toUtc, ct);
            var lost = await _db.Deals.AsNoTracking().CountAsync(d => d.Status == Crm.Domain.Enums.DealStatus.Lost && d.CloseDate >= fromUtc && d.CloseDate <= toUtc, ct);
            var rate = (won + lost) == 0 ? 0 : (double)won / (won + lost);
            return new ConversionItem(won, lost, rate);
        }) ?? new ConversionItem(0, 0, 0);
    }

    public async Task<IReadOnlyList<OwnerSumItem>> GetTopOwnersAsync(Guid? pipelineId, int take = 5, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(Key("owners", pipelineId ?? Guid.Empty, take), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            var q = _db.Deals.AsNoTracking().Where(d => d.Status == Crm.Domain.Enums.DealStatus.Open);
            return await q.GroupBy(d => d.OwnerId)
                .Select(g => new OwnerSumItem(g.Key, g.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Sum)
                .Take(take)
                .ToListAsync(ct);
        }) ?? new List<OwnerSumItem>();
    }
}
