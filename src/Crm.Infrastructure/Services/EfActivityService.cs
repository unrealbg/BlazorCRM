namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfActivityService : IActivityService
    {
        private readonly CrmDbContext _db;

        public EfActivityService(CrmDbContext db) => _db = db;

        public async Task<IEnumerable<Activity>> GetAllAsync(Guid? relatedId = null, CancellationToken ct = default)
        {
            IQueryable<Activity> q = _db.Activities.AsNoTracking();
            if (relatedId is Guid rid)
            {
                q = q.Where(a => a.RelatedId == rid);
            }

            return await q.OrderByDescending(a => a.DueAt).ToListAsync(ct);
        }

        public async Task<Activity> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Activities.FindAsync(new object?[] { id }, ct)
            ?? throw new KeyNotFoundException("Activity not found");

        public async Task<Activity> UpsertAsync(Activity activity, CancellationToken ct = default)
        {
            if (activity.Id == Guid.Empty)
            {
                activity.Id = Guid.NewGuid();
                await _db.Activities.AddAsync(activity, ct);
            }
            else
            {
                var existing = await _db.Activities.AsTracking().FirstOrDefaultAsync(a => a.Id == activity.Id, ct);
                if (existing is null)
                {
                    await _db.Activities.AddAsync(activity, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(activity);
                }
            }

            await _db.SaveChangesAsync(ct);
            return activity;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Activities.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Activities.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
