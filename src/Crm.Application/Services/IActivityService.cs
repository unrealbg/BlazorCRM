namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface IActivityService
    {
        Task<Crm.Application.Common.Models.PagedResult<Activity>> GetPageAsync(
            Guid? relatedId = null,
            Crm.Domain.Enums.ActivityType? type = null,
            Crm.Domain.Enums.ActivityStatus? status = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default);

        Task<Activity> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Activity> UpsertAsync(Activity activity, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
