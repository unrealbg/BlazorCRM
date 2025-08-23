namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetAllAsync(string? filter = null, CancellationToken ct = default);

        Task<TaskItem> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<TaskItem> UpsertAsync(TaskItem task, CancellationToken ct = default);
    }
}
