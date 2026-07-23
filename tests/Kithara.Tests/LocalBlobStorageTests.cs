using Bardie.Module.Channel.Certificates;
using Bardie.Module.Channel.Channel;
using Bardie.Orchestrator.Source;
using Bardie.Orchestrator.Source.Catalog;
using Bardie.Orchestrator.Source.Ports;
using Kithara.Infrastructure.Neck;
using Kithara.Infrastructure.Persistence;
using Kithara.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kithara.Tests;

public class LocalBlobStorageTests
{
    [Fact]
    public async Task Put_round_trip_under_tunes_slug_prefix()
    {
        var root = Path.Combine(Path.GetTempPath(), "kithara-blobs-" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = CreateStorage(root);
            var key = BlobKeyLayout.AssignKey("magpie");
            await using var payload = new MemoryStream("hello-pcm"u8.ToArray());

            var size = await storage.PutAsync(key, payload, "audio/pcm");
            Assert.Equal(9, size);
            Assert.True(await storage.ExistsAsync(key));

            var opened = await storage.OpenReadAsync(key);
            Assert.NotNull(opened);
            await using (opened.Stream)
            {
                Assert.Equal("audio/pcm", opened.ContentType);
                using var reader = new StreamReader(opened.Stream);
                Assert.Equal("hello-pcm", await reader.ReadToEndAsync());
            }

            await storage.DeleteAsync(key);
            Assert.False(await storage.ExistsAsync(key));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("/absolute")]
    [InlineData("tunes/magpie")]
    [InlineData("other/magpie/object")]
    [InlineData("tunes/../magpie/object")]
    public void EnsureValidKey_rejects_escape_and_short_keys(string key)
    {
        Assert.Throws<ArgumentException>(() => BlobKeyLayout.EnsureValidKey(key));
    }

    [Fact]
    public void EnsureKeyOwnedBy_rejects_other_module_prefix()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => BlobKeyLayout.EnsureKeyOwnedBy("tunes/starling/obj", "magpie"));
    }

    [Fact]
    public void ResolvePath_rejects_traversal_even_if_segments_look_valid()
    {
        var root = Path.Combine(Path.GetTempPath(), "kithara-blobs-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // Valid shape but ResolvePath still binds under root; traversal already blocked by EnsureValidKey.
            Assert.Throws<ArgumentException>(() => BlobKeyLayout.ResolvePath(root, "tunes/magpie/../../etc/passwd"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LocalBlobStorage CreateStorage(string root) =>
        new(
            Options.Create(new BlobStorageOptions { Path = root }),
            NullLogger<LocalBlobStorage>.Instance);
}

public class NeckStrunaFifoTests
{
    [Fact]
    public async Task Ensure_and_remove_struna_fifo()
    {
        var root = Path.Combine(Path.GetTempPath(), "kithara-audio-" + Guid.NewGuid().ToString("N"));
        try
        {
            var neck = new Neck(
                Options.Create(new NeckOptions { StrunaFifoRoot = root }),
                new ThrowingDbContextFactory(),
                CreateUnusedSourceOrchestrator(),
                NullLogger<Neck>.Instance);

            var strunaId = Guid.NewGuid();
            var path = await neck.EnsureStrunaFifoAsync(strunaId);
            Assert.Equal(neck.GetStrunaFifoPath(strunaId), path);
            Assert.True(File.Exists(path));

            // Idempotent
            var again = await neck.EnsureStrunaFifoAsync(strunaId);
            Assert.Equal(path, again);

            await neck.RemoveStrunaFifoAsync(strunaId);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static SourceModuleOrchestrator CreateUnusedSourceOrchestrator() =>
        new(
            new SourceModuleCatalog(),
            new UnusedBlobStorage(),
            new UnusedChannelFactory(),
            new UnloadedCertificateStore(),
            new ConfigurationBuilder().Build(),
            NullLogger<SourceModuleOrchestrator>.Instance);

    private sealed class ThrowingDbContextFactory : IDbContextFactory<KitharaDbContext>
    {
        public KitharaDbContext CreateDbContext() =>
            throw new InvalidOperationException("DB not used in FIFO unit test.");

        public Task<KitharaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("DB not used in FIFO unit test.");
    }

    private sealed class UnusedBlobStorage : IBlobStorage
    {
        public Task<long> PutAsync(
            string key,
            Stream content,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<BlobReadResult?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<BlobReadResult?>(null);

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UnusedChannelFactory : IModuleGrpcChannelFactory
    {
        public Grpc.Net.Client.GrpcChannel CreateChannel(
            string address,
            System.Security.Cryptography.X509Certificates.X509Certificate2? clientCertificate = null,
            bool trustRemoteServerCertificate = false,
            bool ownsClientCertificate = false) =>
            throw new InvalidOperationException();
    }

    private sealed class UnloadedCertificateStore : IModuleCertificateStore
    {
        public bool IsLoaded => false;
        public string CaThumbprint => string.Empty;
        public string CaCertificatePem => string.Empty;
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CaCertificate =>
            throw new InvalidOperationException();
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ServerCertificate =>
            throw new InvalidOperationException();
        public System.Security.Cryptography.X509Certificates.X509Certificate2 OpenOutboundClientIdentity() =>
            throw new InvalidOperationException();
        public bool TryGetPresharedClientMaterial(
            string slug,
            out System.Security.Cryptography.X509Certificates.X509Certificate2? certificate)
        {
            certificate = null;
            return false;
        }

        public bool TryGetPresharedClientExpiry(string slug, out DateTimeOffset expiresAt)
        {
            expiresAt = default;
            return false;
        }

        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
