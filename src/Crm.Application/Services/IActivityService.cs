namespace Crm.Application.Services
{
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;

    public interface IActivityService
    {
        Task<PagedResult<Activity>> GetPageAsync(
            PagedRequest request,
            Guid? relatedId = null,
            Crm.Domain.Enums.ActivityType? type = null,
            Crm.Domain.Enums.ActivityStatus? status = null,
            CancellationToken ct = default);

        Task<Activity> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Activity> UpsertAsync(Activity activity, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
