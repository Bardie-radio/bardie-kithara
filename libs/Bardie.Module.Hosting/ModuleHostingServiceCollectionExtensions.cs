using Bardie.Module.Channel.Manifest;
using Bardie.Module.Channel.Participant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bardie.Module.Hosting;

public static class ModuleHostingServiceCollectionExtensions
{
    /// <summary>
    /// Participant DI, Bardie Compose aliases, Kestrel listeners, and optional OTel from the manifest.
    /// Mesh Register/Heartbeat/mTLS stay in <c>Bardie.Module.Channel</c>.
    /// </summary>
    /// <returns>Loaded (and env-overlaid) module manifest.</returns>
    public static ModuleManifest AddBardieModuleHosting(
        this WebApplicationBuilder builder,
        Action<ModuleParticipantOptions>? configure = null,
        bool enableOpenTelemetry = true,
        string otelFallbackServiceName = "bardie.module")
    {
        ArgumentNullException.ThrowIfNull(builder);

        var contentRoot = builder.Environment.ContentRootPath;
        var manifestPath = ModuleManifestLoader.ResolvePath(
            builder.Configuration["MODULE_MANIFEST_PATH"]
            ?? builder.Configuration["ModuleParticipant:ManifestPath"],
            contentRoot);
        var manifest = ModuleManifestLoader.ApplyEnvironmentOverlays(
            ModuleManifestLoader.LoadFromFile(manifestPath),
            builder.Configuration);

        builder.Services.AddSingleton(manifest);
        builder.Services.AddModuleParticipant(
            builder.Configuration,
            configure: configure,
            contentRoot: contentRoot);
        BardieComposeParticipantEnv.Apply(builder.Services, builder.Configuration);

        if (enableOpenTelemetry)
        {
            builder.AddModuleOpenTelemetry(manifest, otelFallbackServiceName);
        }

        var workPort = ModuleHostingPorts.ResolveWorkPort(builder.Configuration);
        var httpPort = ModuleHostingPorts.ResolveHttpPort(builder.Configuration);
        builder.WebHost.ConfigureKestrel(options =>
            options.ConfigureBardieModuleParticipantListeners(httpPort: httpPort, workGrpcPort: workPort));

        return manifest;
    }

    /// <summary>Ensures the work-port server certificate exists before Kestrel accepts work dials.</summary>
    public static Task EnsureModuleParticipantServerCertificateAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var participantStore = app.Services.GetRequiredService<IModuleParticipantCertificateStore>();
        var participantOptions = app.Services.GetRequiredService<IOptions<ModuleParticipantOptions>>().Value;
        return participantStore.EnsureServerCertificateAsync(participantOptions.ServerDnsNames);
    }

    /// <summary>Maps <c>/healthz</c> and <c>/</c> identity JSON endpoints.</summary>
    public static WebApplication MapModuleHostingEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var manifest = app.Services.GetRequiredService<ModuleManifest>();
        app.MapGet("/healthz", () => Results.Ok(new { ok = true, slug = manifest.Slug }));
        app.MapGet("/", () => Results.Ok(new
        {
            service = manifest.OtelServiceName,
            slug = manifest.Slug,
            kind = manifest.Kind,
        }));

        return app;
    }
}
