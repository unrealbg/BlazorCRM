namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfTaskService : ITaskService
    {
        private readonly CrmDbContext _db;

        public EfTaskService(CrmDbContext db) => _db = db;

        public async Task<PagedResult<TaskItem>> SearchAsync(
            PagedRequest request,
            Guid? ownerId = null,
            Crm.Domain.Enums.TaskPriority? priority = null,
            Crm.Domain.Enums.TaskStatus? status = null,
            CancellationToken ct = default)
        {
            IQueryable<TaskItem> q = _db.Tasks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var f = request.Search.Trim();
                q = q.Where(t => EF.Functions.ILike(t.Title, $"%{f}%"));
            }

            if (ownerId is Guid oid)
            {
                q = q.Where(t => t.OwnerId == oid);
            }

            if (priority is not null)
            {
                q = q.Where(t => t.Priority == priority);
            }

            if (status is not null)
            {
                q = q.Where(t => t.Status == status);
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(TaskItem.DueAt) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(TaskItem.DueAt), false) => q.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ThenBy(t => t.Id),
                (nameof(TaskItem.DueAt), true) => q.OrderByDescending(t => t.DueAt ?? DateTime.MinValue).ThenByDescending(t => t.Id),
                (nameof(TaskItem.Priority), false) => q.OrderBy(t => t.Priority).ThenBy(t => t.Id),
                (nameof(TaskItem.Priority), true) => q.OrderByDescending(t => t.Priority).ThenByDescending(t => t.Id),
                (nameof(TaskItem.Status), false) => q.OrderBy(t => t.Status).ThenBy(t => t.Id),
                (nameof(TaskItem.Status), true) => q.OrderByDescending(t => t.Status).ThenByDescending(t => t.Id),
                (nameof(TaskItem.Title), false) => q.OrderBy(t => t.Title).ThenBy(t => t.Id),
                (nameof(TaskItem.Title), true) => q.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),
                (nameof(TaskItem.CreatedAtUtc), false) => q.OrderBy(t => t.CreatedAtUtc).ThenBy(t => t.Id),
                (nameof(TaskItem.CreatedAtUtc), true) => q.OrderByDescending(t => t.CreatedAtUtc).ThenByDescending(t => t.Id),
                _ => q.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ThenBy(t => t.Id)
            };
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<TaskItem>(items, total, page, pageSize);
        }

        public async Task<TaskItem> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Tasks.FindAsync(new object?[] { id }, ct) ?? throw new KeyNotFoundException("Task not found");

        public async Task<TaskItem> UpsertAsync(TaskItem task, CancellationToken ct = default)
        {
            if (task.Id == Guid.Empty)
            {
                task.Id = Guid.NewGuid();
                await _db.Tasks.AddAsync(task, ct);
            }
            else
            {
                var existing = await _db.Tasks.AsTracking().FirstOrDefaultAsync(a => a.Id == task.Id, ct);
                if (existing is null)
                {
                    await _db.Tasks.AddAsync(task, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(task);
                }
            }

            await _db.SaveChangesAsync(ct);
            return task;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Tasks.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Tasks.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
