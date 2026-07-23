using Bardie.Module.Channel.Manifest;
using Bardie.Modules.V1;
using Bardie.Source.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bardie.Module.Source.Tests;

public class SourceModuleLibTests
{
    [Fact]
    public async Task Health_returns_ok()
    {
        var adapter = new StubSource(new ModuleManifest { Slug = "magpie", Kind = "source" });
        var response = await adapter.Health(new HealthRequest(), context: null!);
        Assert.True(response.Ok);
    }

    [Fact]
    public async Task PauseTrack_without_capability_is_failed_precondition()
    {
        var adapter = new StubSource(new ModuleManifest
        {
            Slug = "starling",
            Kind = "source",
            Capabilities = ["search", "play"],
        });

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => adapter.PauseTrack(new PauseTrackRequest { TrackJobId = "x" }, context: null!));
        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task PauseTrack_with_capability_default_is_unimplemented()
    {
        var adapter = new StubSource(new ModuleManifest
        {
            Slug = "magpie",
            Kind = "source",
            Capabilities = ["search", "play", "pause"],
        });

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => adapter.PauseTrack(new PauseTrackRequest { TrackJobId = "x" }, context: null!));
        Assert.Equal(StatusCode.Unimplemented, ex.StatusCode);
    }

    [Fact]
    public void SearchFieldsCustomizer_defaults_to_title()
    {
        var customizer = new SourceSearchFieldsRegisterRequestCustomizer(
            Options.Create(new SourceModuleOptions()));
        var request = new RegisterRequest();
        customizer.Customize(request, new ModuleManifest { Slug = "magpie", Kind = "source" });

        Assert.Equal(RegisterRequest.DetailsOneofCase.Source, request.DetailsCase);
        Assert.Single(request.Source.SearchFields);
        Assert.Equal("title", request.Source.SearchFields[0].Name);
        Assert.True(request.Source.SearchFields[0].Required);
    }

    [Fact]
    public void SearchFieldsCustomizer_prefers_manifest_over_options()
    {
        var manifest = ModuleManifestLoader.LoadFromJson("""
            {
              "slug": "magpie",
              "kind": "source",
              "capabilities": [],
              "source": {
                "searchFields": [
                  { "name": "title", "required": true },
                  { "name": "artist", "required": false }
                ]
              }
            }
            """);

        var customizer = new SourceSearchFieldsRegisterRequestCustomizer(
            Options.Create(new SourceModuleOptions
            {
                SearchFields = [new SourceSearchFieldOptions { Name = "owner", Required = false }],
            }));
        var request = new RegisterRequest();
        customizer.Customize(request, manifest);

        Assert.Equal(2, request.Source.SearchFields.Count);
        Assert.Equal("title", request.Source.SearchFields[0].Name);
        Assert.Equal("artist", request.Source.SearchFields[1].Name);
        Assert.False(request.Source.SearchFields[1].Required);
    }

    [Fact]
    public void TrackJobRegistry_create_get_remove()
    {
        var registry = new TrackJobRegistry();
        var job = registry.Create("struna-1", "ref", "/tmp/a.pcm");
        Assert.True(registry.TryGet(job.TrackJobId, out var found));
        Assert.Equal(job.TrackJobId, found!.TrackJobId);
        Assert.True(registry.TryRemove(job.TrackJobId, out _));
        Assert.False(registry.TryGet(job.TrackJobId, out _));
    }

    [Fact]
    public async Task FifoAudioSink_writes_to_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "bardie-fifo-sink-" + Guid.NewGuid().ToString("N") + ".pcm");
        await File.WriteAllBytesAsync(path, []);
        try
        {
            var sink = new FifoAudioSink();
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            await using var input = new MemoryStream(payload);
            await sink.WriteAsync(path, input);

            var written = await File.ReadAllBytesAsync(path);
            Assert.Equal(payload, written);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubSource : SourceModuleBase
    {
        public StubSource(ModuleManifest manifest)
            : base(manifest)
        {
        }
    }
}
