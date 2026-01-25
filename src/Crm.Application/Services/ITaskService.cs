namespace Crm.Application.Services
{
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;

    public interface ITaskService
    {
        Task<PagedResult<TaskItem>> SearchAsync(
            PagedRequest request,
            Guid? ownerId = null,
            Crm.Domain.Enums.TaskPriority? priority = null,
            Crm.Domain.Enums.TaskStatus? status = null,
            CancellationToken ct = default);

        Task<TaskItem> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<TaskItem> UpsertAsync(TaskItem task, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
