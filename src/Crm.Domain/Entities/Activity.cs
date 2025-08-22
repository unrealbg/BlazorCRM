namespace Crm.Domain.Entities
{
    using Crm.Domain.Enums;

    public class Activity : BaseEntity
    {
        public ActivityType Type { get; set; }

        public RelatedToType RelatedTo { get; set; }

        public Guid? RelatedId { get; set; }

        public DateTime? DueAt { get; set; }

        public ActivityStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}
