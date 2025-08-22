namespace Crm.Infrastructure.Jobs
{
    using Crm.Application.Notifications;
    using Crm.Domain.Enums;
    using Crm.Infrastructure.Notifications;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Quartz;

    [DisallowConcurrentExecution]
    public sealed class RemindersSweepJob : IJob
    {
        private readonly ILogger<RemindersSweepJob> _logger;
        private readonly IServiceProvider _sp;

        public RemindersSweepJob(ILogger<RemindersSweepJob> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationsHub>>();

            var now = DateTime.UtcNow;
            var soon = now.AddMinutes(15);

            // Activities due
            var dueActivities = await db.Activities.AsNoTracking()
                                    .Where(a => a.DueAt != null && a.Status != ActivityStatus.Completed && a.DueAt <= soon)
                                    .OrderBy(a => a.DueAt)
                                    .Take(50)
                                    .ToListAsync();

            foreach (var a in dueActivities)
            {
                var msg = $"Activity {a.Type} due at {a.DueAt:yyyy-MM-dd HH:mm} (Status: {a.Status})";
                await notifier.AddAsync(msg, a.DueAt < now ? "Error" : "Warning");
                await hub.Clients.All.SendAsync("notify", new { title = "Activity", body = msg, severity = a.DueAt < now ? "Error" : "Warning" });
                _logger.LogInformation(msg);
            }

            // Tasks due
            var dueTasks = await db.Tasks.AsNoTracking()
                               .Where(t => t.DueAt != null && t.Status != TaskStatus.Done && t.DueAt <= soon)
                               .OrderBy(t => t.DueAt)
                               .Take(50)
                               .ToListAsync();

            foreach (var t in dueTasks)
            {
                var msg = $"Task '{t.Title}' due {t.DueAt:yyyy-MM-dd HH:mm} (Priority: {t.Priority})";
                await notifier.AddAsync(msg, t.DueAt < now ? "Error" : "Warning");
                await hub.Clients.All.SendAsync("notify", new { title = "Task", body = msg, severity = t.DueAt < now ? "Error" : "Warning" });
                _logger.LogInformation(msg);
            }
        }
    }
}
