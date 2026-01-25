namespace Crm.Web.Tests.Files
{
    using System.Text;
    using Crm.Infrastructure.Files;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;

    public class LocalFileStorageTests
    {
        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "Crm.Web.Tests";
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string WebRootPath { get; set; } = string.Empty;
            public string EnvironmentName { get; set; } = "Testing";
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static LocalFileStorage CreateStorage(string root, long maxBytes = 1024)
        {
            var env = new TestWebHostEnvironment
            {
                ContentRootPath = root,
                WebRootPath = root
            };

            var options = Options.Create(new AttachmentStorageOptions
            {
                UploadsRootPath = root,
                MaxUploadBytes = maxBytes,
                AllowedContentTypes = new[] { "text/plain" }
            });

            return new LocalFileStorage(env, options);
        }

        [Fact]
        public async Task OpenRead_Rejects_PathTraversal()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root);

            var ex1 = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.OpenReadAsync("../secrets.txt", CancellationToken.None));
            var ex2 = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.OpenReadAsync("..\\secrets.txt", CancellationToken.None));

            Assert.Equal(StatusCodes.Status400BadRequest, ex1.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, ex2.StatusCode);
        }

        [Fact]
        public async Task Delete_Rejects_PathTraversal()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root);

            var ex1 = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.DeleteAsync("../secrets.txt", CancellationToken.None));
            var ex2 = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.DeleteAsync("..\\secrets.txt", CancellationToken.None));

            Assert.Equal(StatusCodes.Status400BadRequest, ex1.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, ex2.StatusCode);
        }

        [Fact]
        public async Task Save_Rejects_Max_Size()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root, maxBytes: 10);

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("01234567890"));

            var ex = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.SaveAsync(
                stream,
                "note.txt",
                "text/plain",
                Guid.NewGuid(),
                "demo",
                CancellationToken.None));

            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
        }

        [Fact]
        public async Task Save_Rejects_Disallowed_ContentType()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root, maxBytes: 1024);

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

            var ex = await Assert.ThrowsAsync<AttachmentStorageException>(() => storage.SaveAsync(
                stream,
                "note.txt",
                "application/pdf",
                Guid.NewGuid(),
                "demo",
                CancellationToken.None));

            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
    }
}
