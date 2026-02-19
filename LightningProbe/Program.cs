using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using LightningProbe.Interceptor;
using LightningSentinel.Shared;
using Lnrpc;       // Main LND client namespace
using Routerrpc;    // Router sub-service namespace

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LightningSettings>(
    builder.Configuration.GetSection("LightningSettings"));
// 1. Setup
var lndAddress = "https://127.0.0.1:10009";
var macaroonHex = "0201036c6e64...";

// 2. The Channel (Long-lived)
var channel = GrpcChannel.ForAddress(lndAddress, new GrpcChannelOptions
{
    HttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }
});

// 3. The Authenticated Invoker (Wraps the channel with your "ID Card")
var macaroonInterceptor = new MacaroonInterceptor(macaroonHex);
var authenticatedInvoker = channel.Intercept(macaroonInterceptor);

// 4. Registration (Using the authenticatedInvoker instead of the raw channel)
builder.Services.AddSingleton(new Lnrpc.Lightning.LightningClient(authenticatedInvoker));
builder.Services.AddSingleton(new Routerrpc.Router.RouterClient(authenticatedInvoker));
builder.Services.AddSingleton(new HttpClient());

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();