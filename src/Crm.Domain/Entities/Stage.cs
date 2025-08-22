namespace Crm.Domain.Entities
{
    using Crm.Domain.Common;

    public class Stage : BaseEntity, ITenantOwned
    {
        public Guid TenantId { get; set; }

        public required string Name { get; set; }
        public int Order { get; set; }
        public Guid PipelineId { get; set; }
    }
}
