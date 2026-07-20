using Kithara.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.AddKitharaOpenTelemetry();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.Run();

// Expose for WebApplicationFactory in tests.
public partial class Program;
