namespace Crm.Infrastructure.Files
{
    using Crm.Application.Files;
    using Microsoft.AspNetCore.Hosting;

    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly string _root;
        public LocalFileStorage(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(Stream content, string fileName, string contentType, Guid tenantId, CancellationToken ct)
        {
            var dir = Path.Combine(_root, tenantId.ToString("N"));
            Directory.CreateDirectory(dir);
            var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(dir, $"{Guid.NewGuid():N}_{safeName}");
            using var fs = File.Create(path);
            await content.CopyToAsync(fs, ct);
            return path;
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken ct)
            => Task.FromResult<Stream>(File.OpenRead(path));

        public Task DeleteAsync(string path, CancellationToken ct)
        {
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
