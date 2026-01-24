namespace Crm.Domain.Entities
{
    using Crm.Domain.Common;
    using Crm.Domain.Enums;

    public class Deal : BaseEntity, ITenantOwned, IHasCreatedAt
    {
        public Guid TenantId { get; set; }

        public required string Title { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "";

        public int Probability { get; set; }

        public Guid StageId { get; set; }

        public Guid? OwnerId { get; set; }

        public Guid? CompanyId { get; set; }

        public Guid? ContactId { get; set; }

        public DateTime? CloseDate { get; set; }

        public DealStatus Status { get; set; } = DealStatus.Open;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
