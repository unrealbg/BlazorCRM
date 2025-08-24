namespace Crm.Application.Notifications
{
    public record NotificationItem(string Message, string Severity, DateTime CreatedAtUtc);

    public interface INotificationService
    {
        Task AddAsync(string message, string severity = "Info", CancellationToken ct = default);

        Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int take = 20, CancellationToken ct = default);
    }
}
