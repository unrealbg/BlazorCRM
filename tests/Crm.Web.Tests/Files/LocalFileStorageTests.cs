namespace Crm.Web.Tests.Files
{
    using System.Text;
    using Crm.Infrastructure.Files;
    using Microsoft.AspNetCore.Hosting;
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
                MaxFileSizeBytes = maxBytes,
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

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenReadAsync("../secrets.txt", CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenReadAsync("..\\secrets.txt", CancellationToken.None));
        }

        [Fact]
        public async Task Delete_Rejects_PathTraversal()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root);

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.DeleteAsync("../secrets.txt", CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.DeleteAsync("..\\secrets.txt", CancellationToken.None));
        }

        [Fact]
        public async Task Save_Rejects_Max_Size()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var storage = CreateStorage(root, maxBytes: 10);

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("01234567890"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(
                stream,
                "note.txt",
                "text/plain",
                Guid.NewGuid(),
                "demo",
                CancellationToken.None));
        }
    }
}
