namespace Crm.Web.Components
{
    using Crm.Application.Notifications;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class HeaderBar : IDisposable
    {
        private CancellationTokenSource? _cts;
        private NotificationsPanel? _notificationsPanel;
        private int _unreadCount;

        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [CascadingParameter] 
        IServiceProvider Services { get; set; } = default!;

        [Inject]
        INotificationService NotificationService { get; set; } = default!;

        [Inject]
        Crm.Web.Services.MobileNavState MobileNav { get; set; } = default!;
        protected override async Task OnInitializedAsync()
        {
            _cts = new CancellationTokenSource();
            await UpdateUnreadCountAsync();
        }

        private async Task UpdateUnreadCountAsync()
        {
            if (_cts?.Token.IsCancellationRequested ?? true) return;

            try
            {
                _unreadCount = await NotificationService.GetUnreadCountAsync(_cts.Token);
                StateHasChanged();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        Task ToggleSidebar() => JS.InvokeVoidAsync("toggleSidebar").AsTask();
        Task OpenPalette() => JS.InvokeVoidAsync("openPalette").AsTask();

        void ToggleMobileNav()
        {
            MobileNav.Toggle();
        }

        async Task OpenQuickCreate()
        {
            await JS.InvokeVoidAsync("openModal", "modalQuickCreate");
        }

        async Task OpenNotifications()
        {
            if (_notificationsPanel != null)
            {
                await _notificationsPanel.OpenPanel();
                await UpdateUnreadCountAsync();
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
