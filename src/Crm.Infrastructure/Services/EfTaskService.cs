namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfTaskService : ITaskService
    {
        private readonly CrmDbContext _db;

        public EfTaskService(CrmDbContext db) => _db = db;

        public async Task<Crm.Application.Common.Models.PagedResult<TaskItem>> SearchAsync(
            string? filter = null,
            Guid? ownerId = null,
            Crm.Domain.Enums.TaskPriority? priority = null,
            Crm.Domain.Enums.TaskStatus? status = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            IQueryable<TaskItem> q = _db.Tasks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim();
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

            var ordered = q.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ThenBy(t => t.Id);
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new Crm.Application.Common.Models.PagedResult<TaskItem>(items, total);
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
