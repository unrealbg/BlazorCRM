namespace Crm.Infrastructure.Files
{
    public sealed class AttachmentStorageException : InvalidOperationException
    {
        public int StatusCode { get; }

        public AttachmentStorageException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}