namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Application.Files;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfAttachmentService : IAttachmentService
    {
        private readonly CrmDbContext _db;
        private readonly IFileStorage _storage;
        private readonly Crm.Application.Common.Multitenancy.ITenantProvider _tenant;

        public EfAttachmentService(CrmDbContext db, IFileStorage storage, Crm.Application.Common.Multitenancy.ITenantProvider tenant)
        {
            _db = db;
            _storage = storage;
            _tenant = tenant;
        }

        public async Task<Attachment> UploadAsync(Stream content, string fileName, string contentType, RelatedToType relatedTo, Guid? relatedId, CancellationToken ct = default)
        {
            var path = await _storage.SaveAsync(content, fileName, contentType, _tenant.TenantId, ct);
            var entity = new Attachment { Id = Guid.NewGuid(), FileName = fileName, Size = content.Length, BlobRef = path, ContentType = contentType, RelatedTo = relatedTo, RelatedId = relatedId, TenantId = _tenant.TenantId };
            await _db.Attachments.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<Stream> OpenReadAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Attachments.AsNoTracking().FirstAsync(a => a.Id == id, ct);
            return await _storage.OpenReadAsync(entity.BlobRef!, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Attachments.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(entity.BlobRef))
            {
                await _storage.DeleteAsync(entity.BlobRef, ct);
            }

            _db.Attachments.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<Attachment>> GetForAsync(RelatedToType relatedTo, Guid relatedId, CancellationToken ct = default)
            => await _db.Attachments.AsNoTracking().Where(a => a.RelatedTo == relatedTo && a.RelatedId == relatedId).OrderByDescending(a => a.Id).ToListAsync(ct);
    }
}
