namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface IDealService
    {
        Task<IEnumerable<Deal>> GetAllAsync(Guid? pipelineId = null, Guid? stageId = null, Guid? ownerId = null, CancellationToken ct = default);

        Task<Deal> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Deal> UpsertAsync(Deal deal, CancellationToken ct = default);

        Task<bool> MoveToStageAsync(Guid dealId, Guid stageId, CancellationToken ct = default);

        Task<bool> ChangeOwnerAsync(Guid dealId, Guid? ownerId, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
