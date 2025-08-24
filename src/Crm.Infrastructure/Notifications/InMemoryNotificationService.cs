namespace Crm.Infrastructure.Notifications
{
    using Crm.Application.Notifications;

    public sealed class InMemoryNotificationService : INotificationService
    {
        private readonly LinkedList<NotificationItem> _items = new();
        private readonly object _lock = new();

        public Task AddAsync(string message, string severity = "Info", CancellationToken ct = default)
        {
            lock (_lock)
            {
                _items.AddFirst(new NotificationItem(message, severity, DateTime.UtcNow));

                while (_items.Count > 100)
                {
                    _items.RemoveLast();
                }
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int take = 20, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult<IReadOnlyList<NotificationItem>>(_items.Take(take).ToList());
            }
        }
    }
}
