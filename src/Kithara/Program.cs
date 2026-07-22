using Bardie.Auth.Orchestrator;
using Bardie.ModuleChannel;
using Bardie.ModuleChannel.Certificates;
using Bardie.ModuleChannel.Hosting;
using Bardie.Source.Orchestrator;
using Kithara.Features.Auth;
using Kithara.Features.Library;
using Kithara.Features.Modules;
using Kithara.Infrastructure.Neck;
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

builder.Services.AddKitharaDb(builder.Configuration);
builder.Services.AddKitharaBlobStorage(builder.Configuration);
builder.Services.AddKitharaLibrary();
builder.Services.AddKitharaNeck(builder.Configuration);
builder.Services.AddModuleRegistry(builder.Configuration);
builder.Services.AddKitharaAuthAuthentication(builder.Configuration);
builder.Services.AddHostedService<SeedAdminBootstrapHostedService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadyHealthCheck>("database", tags: ["ready"])
    .AddCheck<ModuleTlsHealthCheck>("module-tls", tags: ["ready"])
    .AddCheck("grpc-listener", () => HealthCheckResult.Healthy("gRPC listener configured on :5000"), tags: ["ready"]);

var app = builder.Build();

var certificateStore = app.Services.GetRequiredService<IModuleCertificateStore>();
await certificateStore.EnsureLoadedAsync().ConfigureAwait(false);

// Ensure guest signing key material exists at boot (mint unused until Phase 6).
_ = app.Services.GetRequiredService<GuestJwtSigningKeyStore>().GetSigningKey();

await app.MigrateKitharaDatabaseAsync().ConfigureAwait(false);

app.UseAuthentication();
app.UseAuthorization();

app.MapKitharaHealthEndpoints();
app.MapAuthEndpoints();
app.MapModuleRegistry();

app.Run();

public partial class Program;
