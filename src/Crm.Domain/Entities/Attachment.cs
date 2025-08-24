namespace Crm.Domain.Entities
{
    using Crm.Domain.Common;
    using Crm.Domain.Enums;

    public class Attachment : BaseEntity, ITenantOwned
    {
        public Guid TenantId { get; set; }

        public required string FileName { get; set; }

        public long Size { get; set; }

        public string? Url { get; set; }

        public string? BlobRef { get; set; }

        public string? ContentType { get; set; }

        public RelatedToType RelatedTo { get; set; }

        public Guid? RelatedId { get; set; }
    }
}
