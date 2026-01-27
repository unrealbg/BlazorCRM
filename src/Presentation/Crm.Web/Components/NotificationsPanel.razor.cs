namespace Crm.Web.Components
{
    using Crm.Application.Notifications;
    using Crm.Web.Services;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class NotificationsPanel : IDisposable
    {
        private CancellationTokenSource? _cts;

        [Inject]
        public INotificationService NotificationService { get; set; } = default!;

        [Inject]
        public NotificationsService SignalRService { get; set; } = default!;

        [Inject]
        public IJSRuntime JS { get; set; } = default!;

        public List<NotificationItem> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _cts = new CancellationTokenSource();

            SignalRService.Received += OnNotificationReceived;

            await SignalRService.StartAsync();

            await LoadNotificationsAsync();
        }

        private void OnNotificationReceived(NotificationDto dto)
        {
            InvokeAsync(async () =>
            {
                await LoadNotificationsAsync();
                StateHasChanged();
            });
        }

        private async Task LoadNotificationsAsync()
        {
            if (_cts?.Token.IsCancellationRequested ?? true) return;

            try
            {
                var items = await NotificationService.GetRecentAsync(50, _cts.Token);
                Notifications = items.ToList();
                UnreadCount = await NotificationService.GetUnreadCountAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        public async Task OpenPanel()
        {
            await LoadNotificationsAsync();
            await JS.InvokeVoidAsync("eval", "document.getElementById('notificationsPanel')?.classList.remove('hidden')");
            StateHasChanged();
        }

        private async Task ClosePanel()
        {
            await JS.InvokeVoidAsync("eval", "document.getElementById('notificationsPanel')?.classList.add('hidden')");
        }

        private async Task MarkAsRead(Guid id)
        {
            if (_cts?.Token.IsCancellationRequested ?? true) return;

            try
            {
                await NotificationService.MarkAsReadAsync(id, _cts.Token);
                await LoadNotificationsAsync();
                StateHasChanged();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        private string FormatTimestamp(DateTime utc)
        {
            var local = utc.ToLocalTime();
            var diff = DateTime.Now - local;

            if (diff.TotalMinutes < 1)
            {
                return "Just now";
            }

            if (diff.TotalMinutes < 60)
            {
                return $"{(int)diff.TotalMinutes}m ago";
            }

            if (diff.TotalHours < 24)
            {
                return $"{(int)diff.TotalHours}h ago";
            }

            if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays}d ago";
            }

            return local.ToString("MMM d, yyyy");
        }

        public void Dispose()
        {
            SignalRService.Received -= OnNotificationReceived;
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
