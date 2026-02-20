using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using LightningProbe.Interceptor;
using LightningSentinel.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
})
.AddServiceDiscovery();

// 1. Load the Settings
var lightningSettings = builder.Configuration
    .GetSection("LightningSettings")
    .Get<LightningSettings>();

// 2. Setup the Channel using Settings
var channel = GrpcChannel.ForAddress(lightningSettings.LNDAddress, new GrpcChannelOptions
{
    HttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }
});

// 3. Authenticate with the Macaroon from Settings
var macaroonInterceptor = new MacaroonInterceptor(lightningSettings.MacaroonHex);
var authenticatedInvoker = channel.Intercept(macaroonInterceptor);

// 4. Registration (Using the authenticatedInvoker instead of the raw channel)
builder.Services.Configure<LightningSettings>(builder.Configuration.GetSection("LightningSettings"));
builder.Services.AddSingleton(new Lnrpc.Lightning.LightningClient(authenticatedInvoker));
builder.Services.AddSingleton(new Routerrpc.Router.RouterClient(authenticatedInvoker));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();