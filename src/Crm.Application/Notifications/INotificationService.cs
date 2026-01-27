namespace Crm.Application.Notifications
{
    public record NotificationItem(Guid Id, string Message, string Severity, DateTime CreatedAtUtc, bool IsRead);

    public interface INotificationService
    {
        Task AddAsync(string message, string severity = "Info", CancellationToken ct = default);

        Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int take = 20, CancellationToken ct = default);

        Task MarkAsReadAsync(Guid id, CancellationToken ct = default);

        Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    }
}
