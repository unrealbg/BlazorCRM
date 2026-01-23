namespace Crm.Web.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

public sealed record NotificationDto(string Title, string Body, string Severity);

public sealed class NotificationsService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hub;
    public event Action<NotificationDto>? Received;
    public NotificationsService(NavigationManager nav) => _nav = nav;

    public async Task StartAsync()
    {
        if (_hub is not null && _hub.State == HubConnectionState.Connected)
        {
            return;
        }

        _hub = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<NotificationDto>("notify", n => Received?.Invoke(n));
        await _hub.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }
}
