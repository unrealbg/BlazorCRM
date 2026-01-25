namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfDealService : IDealService
    {
        private readonly CrmDbContext _db;

        public EfDealService(CrmDbContext db) => _db = db;

        public async Task<PagedResult<Deal>> GetPageAsync(
            PagedRequest request,
            Guid? pipelineId = null,
            Guid? stageId = null,
            Guid? ownerId = null,
            CancellationToken ct = default)
        {
            IQueryable<Deal> q = _db.Deals.AsNoTracking();
            if (stageId is Guid sid)
            {
                q = q.Where(d => d.StageId == sid);
            }

            if (ownerId is Guid oid)
            {
                q = q.Where(d => d.OwnerId == oid);
            }

            if (pipelineId is Guid pid)
            {
                var stageIds = _db.Stages.Where(s => s.PipelineId == pid).Select(s => s.Id);
                q = q.Where(d => stageIds.Contains(d.StageId));
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 200 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Deal.Title) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Deal.Title), false) => q.OrderBy(d => d.Title).ThenBy(d => d.Id),
                (nameof(Deal.Title), true) => q.OrderByDescending(d => d.Title).ThenByDescending(d => d.Id),
                (nameof(Deal.Amount), false) => q.OrderBy(d => d.Amount).ThenBy(d => d.Id),
                (nameof(Deal.Amount), true) => q.OrderByDescending(d => d.Amount).ThenByDescending(d => d.Id),
                (nameof(Deal.Probability), false) => q.OrderBy(d => d.Probability).ThenBy(d => d.Id),
                (nameof(Deal.Probability), true) => q.OrderByDescending(d => d.Probability).ThenByDescending(d => d.Id),
                (nameof(Deal.CloseDate), false) => q.OrderBy(d => d.CloseDate ?? DateTime.MaxValue).ThenBy(d => d.Id),
                (nameof(Deal.CloseDate), true) => q.OrderByDescending(d => d.CloseDate ?? DateTime.MinValue).ThenByDescending(d => d.Id),
                (nameof(Deal.CreatedAtUtc), false) => q.OrderBy(d => d.CreatedAtUtc).ThenBy(d => d.Id),
                (nameof(Deal.CreatedAtUtc), true) => q.OrderByDescending(d => d.CreatedAtUtc).ThenByDescending(d => d.Id),
                _ => q.OrderBy(d => d.Title).ThenBy(d => d.Id)
            };
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<Deal>(items, total, page, pageSize);
        }

        public async Task<Deal> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Deals.FindAsync(new object?[] { id }, ct) ?? throw new KeyNotFoundException("Deal not found");

        public async Task<Deal> UpsertAsync(Deal deal, CancellationToken ct = default)
        {
            if (deal.Id == Guid.Empty)
            {
                deal.Id = Guid.NewGuid();
                await _db.Deals.AddAsync(deal, ct);
            }
            else
            {
                var existing = await _db.Deals.AsTracking().FirstOrDefaultAsync(d => d.Id == deal.Id, ct);
                if (existing is null)
                {
                    await _db.Deals.AddAsync(deal, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(deal);
                }
            }

            await _db.SaveChangesAsync(ct);
            return deal;
        }

        public async Task<bool> MoveToStageAsync(Guid dealId, Guid stageId, CancellationToken ct = default)
        {
            var deal = await _db.Deals.FindAsync(new object?[] { dealId }, ct);
            if (deal is null)
            {
                return false;
            }

            deal.StageId = stageId;
            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> ChangeOwnerAsync(Guid dealId, Guid? ownerId, CancellationToken ct = default)
        {
            var deal = await _db.Deals.FindAsync(new object?[] { dealId }, ct);
            if (deal is null)
            {
                return false;
            }

            deal.OwnerId = ownerId;
            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Deals.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Deals.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return true;
        }
    }
}
