namespace Crm.Infrastructure.Auditing
{
    public class AuditEntry
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string? UserId { get; set; }

        public string Entity { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? ChangesJson { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? CorrelationId { get; set; }
    }
}
