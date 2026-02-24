using LightningSentinel.Shared.LightningProbe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup service defaults & Aspire
builder.AddServiceDefaults();

// 2. Add Controller Support (REQUIRED)
builder.Services.AddControllers();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("SentinelDb");

builder.Services.AddDbContext<SentinelDbContext>(options =>
    options.UseNpgsql(connectionString, x =>
        x.MigrationsHistoryTable("__EFMigrationsHistory", "sentinel")));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // This creates the JSON at /openapi/v1.json
    app.MapOpenApi();

    // This creates the visual dashboard at /scalar/v1
    app.MapScalarApiReference();
}
// 3. Map the Controller Routes (REQUIRED)
app.MapControllers();

// Root message so you know it's alive
app.MapGet("/", () => "Lightning Sentinel API is running. Send probes to /api/v1/probes");

app.MapDefaultEndpoints();

app.Run();