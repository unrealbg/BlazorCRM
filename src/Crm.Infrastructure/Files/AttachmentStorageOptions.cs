namespace Crm.Infrastructure.Files
{
    public sealed class AttachmentStorageOptions
    {
        public string? UploadsRootPath { get; set; }

        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        public string[] AllowedContentTypes { get; set; } = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/gif",
            "text/plain",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
