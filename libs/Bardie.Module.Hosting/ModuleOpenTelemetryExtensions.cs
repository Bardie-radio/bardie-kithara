using Bardie.Module.Channel.Manifest;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bardie.Module.Hosting;

public static class ModuleOpenTelemetryExtensions
{
    /// <summary>
    /// Registers OTLP traces/metrics with <c>service.name</c> from <see cref="ModuleManifest.OtelServiceName"/>.
    /// </summary>
    public static WebApplicationBuilder AddModuleOpenTelemetry(
        this WebApplicationBuilder builder,
        ModuleManifest manifest,
        string fallbackServiceName = "bardie.module")
    {
        var serviceName = string.IsNullOrWhiteSpace(manifest.OtelServiceName)
            ? fallbackServiceName
            : manifest.OtelServiceName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(ModuleOpenTelemetryExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}
