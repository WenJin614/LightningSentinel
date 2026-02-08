var builder = Host.CreateApplicationBuilder(args);

// Register a shared HttpClient for Worker (avoid needing the HttpClientFactory package)
builder.Services.AddSingleton(new HttpClient());

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
