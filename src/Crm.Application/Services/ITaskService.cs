namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface ITaskService
    {
        Task<Crm.Application.Common.Models.PagedResult<TaskItem>> SearchAsync(
            string? filter = null,
            Guid? ownerId = null,
            Crm.Domain.Enums.TaskPriority? priority = null,
            Crm.Domain.Enums.TaskStatus? status = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default);

        Task<TaskItem> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<TaskItem> UpsertAsync(TaskItem task, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
