using Google.Protobuf.WellKnownTypes;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.LightningSentinel_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.LightningSentinel_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.LightningProbe>("lightningprobe")
       .WithReference(apiService);

builder.Build().Run();
