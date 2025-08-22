namespace Crm.Domain.Entities
{
    using Crm.Domain.Enums;

    public class TaskItem : BaseEntity
    {
        public required string Title { get; set; }

        public DateTime? DueAt { get; set; }

        public Guid? OwnerId { get; set; }

        public RelatedToType RelatedTo { get; set; }

        public Guid? RelatedId { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public Crm.Domain.Enums.TaskStatus Status { get; set; } = Crm.Domain.Enums.TaskStatus.Todo;
    }
}
