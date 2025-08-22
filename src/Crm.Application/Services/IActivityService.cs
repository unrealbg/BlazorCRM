namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface IActivityService
    {
        Task<IEnumerable<Activity>> GetAllAsync(Guid? relatedId = null, CancellationToken ct = default);

        Task<Activity> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Activity> UpsertAsync(Activity activity, CancellationToken ct = default);
    }
}
