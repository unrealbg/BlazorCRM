namespace Crm.Infrastructure.Files
{
    using Crm.Application.Files;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Options;

    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly string _root;
        private readonly string _rootPrefix;
        private readonly AttachmentStorageOptions _options;
        private readonly StringComparison _pathComparison;
        private readonly long _maxUploadBytes;
        private readonly int _maxFileNameLength;

        public LocalFileStorage(IWebHostEnvironment env, IOptions<AttachmentStorageOptions> options)
        {
            _options = options.Value;
            _maxUploadBytes = _options.MaxUploadBytes > 0
                ? _options.MaxUploadBytes
                : (_options.MaxFileSizeBytes > 0 ? _options.MaxFileSizeBytes : 10 * 1024 * 1024);
            _maxFileNameLength = _options.MaxFileNameLength > 0 ? _options.MaxFileNameLength : 120;

            var baseRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var configured = _options.UploadsRootPath;
            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(baseRoot, "uploads")
                : (Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(env.ContentRootPath ?? Directory.GetCurrentDirectory(), configured));

            _root = Path.GetFullPath(root);
            _rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
                ? _root
                : _root + Path.DirectorySeparatorChar;
            _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(Stream content, string fileName, string contentType, Guid tenantId, string tenantSlug, CancellationToken ct)
        {
            ValidateContentType(contentType);

            var safeTenant = SanitizeSegment(string.IsNullOrWhiteSpace(tenantSlug) ? tenantId.ToString("N") : tenantSlug);
            var safeName = SanitizeFileName(fileName, _maxFileNameLength);
            var year = DateTime.UtcNow.ToString("yyyy");
            var relativePath = Path.Combine(safeTenant, year, $"{Guid.NewGuid():N}_{safeName}");
            var relativeForStorage = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = GetFullPathFromRelative(relativeForStorage);
            var dir = Path.GetDirectoryName(fullPath) ?? _root;
            Directory.CreateDirectory(dir);

            using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await CopyToAsyncWithLimit(content, fs, ct);
            return relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken ct)
        {
            var normalized = NormalizeExistingPath(path, out var relative);
            var fullPath = GetFullPathFromRelative(normalized ? relative : path);
            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public Task DeleteAsync(string path, CancellationToken ct)
        {
            var normalized = NormalizeExistingPath(path, out var relative);
            var fullPath = GetFullPathFromRelative(normalized ? relative : path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetFullPathFromRelative(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new AttachmentStorageException("Invalid attachment path.", StatusCodes.Status400BadRequest);
            }

            if (Path.IsPathRooted(relativePath))
            {
                throw new AttachmentStorageException("Absolute attachment paths are not allowed.", StatusCodes.Status400BadRequest);
            }

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_root, normalized));

            if (!fullPath.StartsWith(_rootPrefix, _pathComparison))
            {
                throw new AttachmentStorageException("Attachment path traversal detected.", StatusCodes.Status400BadRequest);
            }

            return fullPath;
        }

        public bool NormalizeExistingPath(string path, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new AttachmentStorageException("Invalid attachment path.", StatusCodes.Status400BadRequest);
            }

            if (!Path.IsPathRooted(path))
            {
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(_rootPrefix, _pathComparison))
            {
                throw new AttachmentStorageException("Attachment path is outside uploads root.", StatusCodes.Status400BadRequest);
            }

            var relative = Path.GetRelativePath(_root, fullPath);
            relativePath = relative.Replace(Path.DirectorySeparatorChar, '/');
            return true;
        }

        private void ValidateContentType(string contentType)
        {
            if (_options.AllowedContentTypes is { Length: > 0 })
            {
                var allowed = new HashSet<string>(_options.AllowedContentTypes, StringComparer.OrdinalIgnoreCase);
                if (!allowed.Contains(contentType))
                {
                    throw new AttachmentStorageException("Attachment content type is not allowed.", StatusCodes.Status415UnsupportedMediaType);
                }
            }
        }

        private async Task CopyToAsyncWithLimit(Stream source, Stream destination, CancellationToken ct)
        {
            var max = _maxUploadBytes;
            if (max > 0 && source.CanSeek && source.Length > max)
            {
                throw new AttachmentStorageException($"Attachment exceeds max size of {max} bytes.", StatusCodes.Status413PayloadTooLarge);
            }

            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (max > 0 && total > max)
                {
                    throw new AttachmentStorageException($"Attachment exceeds max size of {max} bytes.", StatusCodes.Status413PayloadTooLarge);
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }

        private static string SanitizeSegment(string segment)
        {
            var name = Path.GetFileName(segment);
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(name) ? "tenant" : name;
        }

        public static string SanitizeFileName(string fileName, int maxLength)
        {
            var name = Path.GetFileName(fileName);
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(name))
            {
                return "file";
            }

            if (maxLength > 0 && name.Length > maxLength)
            {
                name = name[..maxLength];
            }

            return name;
        }
    }
}
