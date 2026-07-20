using Bardie.Auth.Orchestrator;
using Bardie.ModuleChannel;
using Bardie.ModuleChannel.Certificates;
using Bardie.ModuleChannel.Hosting;
using Bardie.Source.Orchestrator;
using Kithara.Features.Modules;
using Kithara.Infrastructure.Observability;
using Kithara.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.WebHost.ConfigureKestrel(options => options.ConfigureBardieModuleListeners());

builder.AddKitharaOpenTelemetry();

builder.Services.AddModuleChannel(builder.Configuration);
builder.Services.AddAuthModuleOrchestrator(registerModuleChannel: false);
builder.Services.AddSourceModuleOrchestrator(registerModuleChannel: false);

builder.Services.AddKitharaPersistence(builder.Configuration);
builder.Services.AddModuleRegistry(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadyHealthCheck>("database", tags: ["ready"])
    .AddCheck<ModuleTlsHealthCheck>("module-tls", tags: ["ready"])
    .AddCheck("grpc-listener", () => HealthCheckResult.Healthy("gRPC listener configured on :5000"), tags: ["ready"]);

var app = builder.Build();

var certificateStore = app.Services.GetRequiredService<IModuleCertificateStore>();
await certificateStore.EnsureLoadedAsync().ConfigureAwait(false);

await app.MigrateKitharaDatabaseAsync().ConfigureAwait(false);

app.MapKitharaHealthEndpoints();
app.MapModuleRegistry();

app.Run();

public partial class Program;
