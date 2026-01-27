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
                _items.AddFirst(new NotificationItem(Guid.NewGuid(), message, severity, DateTime.UtcNow, false));

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

        public Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var node = _items.First;
                while (node != null)
                {
                    if (node.Value.Id == id)
                    {
                        var prev = node.Previous;
                        _items.Remove(node);
                        var updated = node.Value with { IsRead = true };
                        if (prev != null)
                            _items.AddAfter(prev, updated);
                        else
                            _items.AddFirst(updated);
                        break;
                    }
                    node = node.Next;
                }
            }
            return Task.CompletedTask;
        }

        public Task<int> GetUnreadCountAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_items.Count(i => !i.IsRead));
            }
        }
    }
}
