namespace Crm.Domain.Entities
{
    using Crm.Domain.Common;

    public class Pipeline : BaseEntity, ITenantOwned
    {
        public Guid TenantId { get; set; }

        public required string Name { get; set; }
    }
}
