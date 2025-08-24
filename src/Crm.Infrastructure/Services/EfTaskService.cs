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

        public async Task<IEnumerable<TaskItem>> GetAllAsync(string? filter = null, CancellationToken ct = default)
        {
            IQueryable<TaskItem> q = _db.Tasks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim().ToLowerInvariant();
                q = q.Where(t => EF.Functions.ILike(t.Title, $"%{f}%"));
            }

            return await q.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ToListAsync(ct);
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
