# Mobile Notifications Feature

## Overview
Full-screen mobile notifications panel with SignalR real-time updates, graceful reconnection handling, and mark-as-read functionality.

## Components

### NotificationsPanel.razor
- **Mobile**: Full-screen panel with bottom sheet behavior
- **Desktop**: Centered modal (480px max-width, 70vh max-height)
- **Features**:
  - Real-time notification updates via SignalR
  - Mark individual notifications as read
  - Severity chips (Info, Success, Warning, Error)
  - Relative timestamps (e.g., "5m ago", "2h ago")
  - Empty state with icon
  - Unread count badge in header

### HeaderBar.razor
- Bell icon with unread count badge
- Opens NotificationsPanel on click
- Badge hidden when count is 0
- Refreshes unread count when panel opens

### NotificationsService.cs
- SignalR hub connection with automatic reconnect
- Reconnect intervals: 0s, 2s, 5s, 10s
- Logging level set to Warning (no spam)
- Connection lifecycle events logged appropriately

### INotificationService / InMemoryNotificationService
- **AddAsync**: Add notification with severity
- **GetRecentAsync**: Retrieve latest N notifications
- **MarkAsReadAsync**: Mark notification as read by ID
- **GetUnreadCountAsync**: Get count of unread notifications
- In-memory storage (max 100 items)

## Styling

### Mobile (< 768px)
- Full-screen overlay with backdrop blur
- Bottom sheet slides up
- Rounded top corners (24px)
- Touch-optimized spacing

### Desktop (>= 768px)
- Centered modal
- Rounded corners (24px)
- Max width: 480px
- Max height: 70vh

### Severity Colors
- **Info**: Blue (`#2563eb`)
- **Success**: Green (`#059669`)
- **Warning**: Orange (`#d97706`)
- **Error**: Red (`#dc2626`)
- Dark mode variants included

## Usage

### Sending Notifications (Server-side)
```csharp
// Inject services
INotificationService notifSvc
IHubContext<NotificationsHub> hub

// Add to storage
await notifSvc.AddAsync("Deal won!", "Success");

// Broadcast to all connected clients
await hub.Clients.All.SendAsync("notify", new {
    Title = "CRM",
    Body = "Deal won!",
    Severity = "Success"
});
```

### Testing Notifications (Development)
```bash
# Using dev endpoint (only available in Development environment)
curl -X POST "http://localhost:5000/api/dev/notify?message=Test&severity=Info"
```

## API

### SignalR Hub
- **Endpoint**: `/hubs/notifications`
- **Method**: `notify(NotificationDto)`
- **NotificationDto**: `{ Title: string, Body: string, Severity: string }`

### Dev Endpoint (Development only)
- **POST** `/api/dev/notify?message=string&severity=string`
- **Returns**: `{ message: "Notification sent" }`
- Adds notification to storage and broadcasts to all clients

## Configuration

### Reconnection Policy
```csharp
.WithAutomaticReconnect(new[] {
    TimeSpan.Zero,           // Immediate
    TimeSpan.FromSeconds(2), // After 2s
    TimeSpan.FromSeconds(5), // After 5s
    TimeSpan.FromSeconds(10) // After 10s
})
```

### Logging
```csharp
.ConfigureLogging(logging => {
    logging.SetMinimumLevel(LogLevel.Warning);
})
```

## Future Enhancements
- Persistent storage (Database)
- User-specific notifications (per-tenant filtering)
- Push notifications for mobile apps
- Notification preferences/settings
- Bulk mark-all-as-read
- Notification categories/filters
