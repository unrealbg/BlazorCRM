namespace Crm.Application.Services
{
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;

    public interface IAttachmentService
    {
        Task<Attachment> UploadAsync(Stream content, string fileName, string contentType, RelatedToType relatedTo, Guid? relatedId, CancellationToken ct = default);

        Task<Stream> OpenReadAsync(Guid id, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<IEnumerable<Attachment>> GetForAsync(RelatedToType relatedTo, Guid relatedId, CancellationToken ct = default);
    }
}
