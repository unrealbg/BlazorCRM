namespace Crm.Application.Files
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string fileName, string contentType, Guid tenantId, CancellationToken ct);

        Task<Stream> OpenReadAsync(string path, CancellationToken ct);

        Task DeleteAsync(string path, CancellationToken ct);
    }
}
