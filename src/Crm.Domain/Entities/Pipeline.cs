namespace Crm.Domain.Entities
{
    public class Pipeline : BaseEntity
    {
        public required string Name { get; set; }

        public Guid TenantId { get; set; }
    }
}
