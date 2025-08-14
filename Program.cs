using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json and environment variables
builder.Configuration.AddJsonFile("appsettings.json", optional: true)
                     .AddEnvironmentVariables();

// Get DB provider and connection string from config/env
var dbProvider = builder.Configuration["DbProvider"] ?? "sqlite";
var connectionString = builder.Configuration["DbConnectionString"] ?? "Data Source=kithara.db";

// Configure EF Core dynamically
builder.Services.AddDbContext<KitharaDbContext>(options =>
{
    switch (dbProvider.ToLower())
    {
        case "postgres":
        case "postgresql":
            options.UseNpgsql(connectionString);
            break;
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        default:
            throw new InvalidOperationException($"Unsupported DB provider: {dbProvider}");
    }
});


// Register NeckService for audio stream management
builder.Services.AddSingleton<INeckService, NeckService>();

builder.Services.AddControllers();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Pass DbContext to endpoints


app.Run();

