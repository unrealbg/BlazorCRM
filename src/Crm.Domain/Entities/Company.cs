namespace Crm.Domain.Entities
{
    using Crm.Domain.Common;

    public class Company : BaseEntity, ITenantOwned, IHasCreatedAt
    {
        public Guid TenantId { get; set; }
        public required string Name { get; set; }
        public string? Industry { get; set; }
        public string? Address { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
