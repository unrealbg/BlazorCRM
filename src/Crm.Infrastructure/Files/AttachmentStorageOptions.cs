namespace Crm.Infrastructure.Files
{
    public sealed class AttachmentStorageOptions
    {
        public string? UploadsRootPath { get; set; }

        public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

        // Legacy setting (kept for backward compatibility)
        public long MaxFileSizeBytes { get; set; }

        public int MaxFileNameLength { get; set; } = 120;

        public string[] AllowedContentTypes { get; set; } = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "text/plain",
            "text/csv",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
