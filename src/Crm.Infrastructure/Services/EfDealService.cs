namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfDealService : IDealService
    {
        private readonly CrmDbContext _db;

        public EfDealService(CrmDbContext db) => _db = db;

        public async Task<IEnumerable<Deal>> GetAllAsync(Guid? pipelineId = null, Guid? stageId = null, Guid? ownerId = null, CancellationToken ct = default)
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

            return await q.OrderBy(d => d.Title).ToListAsync(ct);
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
                if (existing is null) await _db.Deals.AddAsync(deal, ct);
                else _db.Entry(existing).CurrentValues.SetValues(deal);
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
    }
}
