namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfActivityService : IActivityService
    {
        private readonly CrmDbContext _db;

        public EfActivityService(CrmDbContext db) => _db = db;

        public async Task<PagedResult<Activity>> GetPageAsync(
            PagedRequest request,
            Guid? relatedId = null,
            Crm.Domain.Enums.ActivityType? type = null,
            Crm.Domain.Enums.ActivityStatus? status = null,
            CancellationToken ct = default)
        {
            IQueryable<Activity> q = _db.Activities.AsNoTracking();
            if (relatedId is Guid rid)
            {
                q = q.Where(a => a.RelatedId == rid);
            }

            if (type is not null)
            {
                q = q.Where(a => a.Type == type);
            }

            if (status is not null)
            {
                q = q.Where(a => a.Status == status);
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Activity.DueAt) : request.SortBy;
            var desc = string.IsNullOrWhiteSpace(request.SortDir) && string.IsNullOrWhiteSpace(request.SortBy)
                ? true
                : string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Activity.DueAt), false) => q.OrderBy(a => a.DueAt ?? DateTime.MaxValue).ThenBy(a => a.Id),
                (nameof(Activity.DueAt), true) => q.OrderByDescending(a => a.DueAt ?? DateTime.MaxValue).ThenByDescending(a => a.Id),
                (nameof(Activity.CreatedAtUtc), false) => q.OrderBy(a => a.CreatedAtUtc).ThenBy(a => a.Id),
                (nameof(Activity.CreatedAtUtc), true) => q.OrderByDescending(a => a.CreatedAtUtc).ThenByDescending(a => a.Id),
                (nameof(Activity.Status), false) => q.OrderBy(a => a.Status).ThenBy(a => a.Id),
                (nameof(Activity.Status), true) => q.OrderByDescending(a => a.Status).ThenByDescending(a => a.Id),
                (nameof(Activity.Type), false) => q.OrderBy(a => a.Type).ThenBy(a => a.Id),
                (nameof(Activity.Type), true) => q.OrderByDescending(a => a.Type).ThenByDescending(a => a.Id),
                _ => q.OrderByDescending(a => a.DueAt ?? DateTime.MaxValue).ThenByDescending(a => a.Id)
            };
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<Activity>(items, total, page, pageSize);
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
