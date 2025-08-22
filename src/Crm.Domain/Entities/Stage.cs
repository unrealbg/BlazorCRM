namespace Crm.Domain.Entities
{
    public class Stage : BaseEntity
    {
        public required string Name { get; set; }

        public int Order { get; set; }

        public Guid PipelineId { get; set; }
    }
}
