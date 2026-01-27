namespace Crm.Web.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

public sealed record NotificationDto(string Title, string Body, string Severity);

public sealed class NotificationsService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private readonly ILogger<NotificationsService> _logger;
    private HubConnection? _hub;
    private bool _isStarting;

    public event Action<NotificationDto>? Received;

    public NotificationsService(NavigationManager nav, ILogger<NotificationsService> logger)
    {
        _nav = nav;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_isStarting) return;
        
        if (_hub is not null && _hub.State == HubConnectionState.Connected)
        {
            return;
        }

        _isStarting = true;

        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(_nav.ToAbsoluteUri("/hubs/notifications"))
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .Build();

            _hub.Reconnecting += error =>
            {
                _logger.LogInformation("SignalR reconnecting...");
                return Task.CompletedTask;
            };

            _hub.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR reconnected");
                return Task.CompletedTask;
            };

            _hub.Closed += error =>
            {
                _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
                return Task.CompletedTask;
            };

            _hub.On<NotificationDto>("notify", n => Received?.Invoke(n));
            
            await _hub.StartAsync();
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
        }
        finally
        {
            _isStarting = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }
}
