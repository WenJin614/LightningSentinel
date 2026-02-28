using Grpc.Core;
using LightningSentinel.Shared;
using LightningSentinel.Shared.LightningProbe;
using Lnrpc;
using Microsoft.Extensions.Options;
using Routerrpc;
using System.Net.Http.Json;
using System.Runtime;
using System.Security.Cryptography;
using Sentinel.Grpc;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly LightningSettings _lightningSettings;
    private readonly ProbeGrpcService.ProbeGrpcServiceClient _grpcClient;

    // NATIVE gRPC CLIENTS
    private readonly Lnrpc.Lightning.LightningClient _lndClient;
    private readonly Routerrpc.Router.RouterClient _routerClient;

    public Worker(
        ILogger<Worker> logger,
        IHttpClientFactory clientFactory, // Inject the Factory instead
        IOptions<LightningSettings> lightningSettings,
        Lnrpc.Lightning.LightningClient lndClient,
        Routerrpc.Router.RouterClient routerClient,
        ProbeGrpcService.ProbeGrpcServiceClient grpcClient)
    {
        _logger = logger;
        // Explicitly ask for the named client
        _httpClient = clientFactory.CreateClient("api");
        _lightningSettings = lightningSettings.Value;
        _lndClient = lndClient;
        _routerClient = routerClient;
        _grpcClient = grpcClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting probe cycle at: {time}", DateTimeOffset.Now);

            // 1. Perform the Probe
            var result = await SentinelCheck(_lightningSettings.KarakenNode, deepProbe: false);

            // 2. Map your result to the gRPC Request message
            var request = new ProbeRequest
            {
                PubKey = result.PubKey,
                IsAlive = result.IsAlive,
                LatencyMs = result.LatencyMs,
                CheckedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            try
            {
                // 3. Call the gRPC Service instead of PostAsJsonAsync
                var response = await _grpcClient.SendProbeResultAsync(request, cancellationToken: stoppingToken);

                _logger.LogInformation("gRPC Response: {success}", response.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC Call failed");
            }
            _logger.LogInformation("Probe sent to Sentinel. Status: {status}", result.IsAlive);

            // Wait 5 minutes before the next check
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    public async Task<ProbeResult> SentinelCheck(string pubKey, bool deepProbe = false)
    {
        // 1. Always try the Cheap Check first (QueryRoutes)
        var routeResult = await CheckRouteAvailability(pubKey);

        // 2. If QueryRoutes says it's down, OR if we want to "Double Check"
        if (!routeResult.IsAlive || deepProbe)
        {
            // Only run the Fake Hash Probe if we really need to verify plumbing
            return await PerformFakeHashProbe(pubKey);
        }

        return routeResult;
    }

    // METHOD 1: The Cheap "Gossip" Check
    private async Task<ProbeResult> CheckRouteAvailability(string pubKey)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var request = new QueryRoutesRequest
            {
                PubKey = pubKey,
                Amt = 1000, // Check if a 1k sat path is possible
                UseMissionControl = true
            };
            var resp = await _lndClient.QueryRoutesAsync(request);
            return new ProbeResult(pubKey, resp.Routes.Any(), (int)sw.ElapsedMilliseconds, DateTime.UtcNow);
        }
        catch
        {
            return new ProbeResult(pubKey, false, 0, DateTime.UtcNow, "Gossip lookup failed");
        }
    }

    // METHOD 2: The Physical "Plumbing" Check (Fake Hash)
    private async Task<ProbeResult> PerformFakeHashProbe(string pubKey)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        byte[] fakeHash = new byte[32];
        RandomNumberGenerator.Fill(fakeHash);

        try
        {
            // 1. Convert hex string to a standard byte array
            byte[] destBytes = Convert.FromHexString(pubKey);

            // 2. Wrap that byte array in a Protobuf ByteString
            var request = new SendPaymentRequest
            {
                Dest = Google.Protobuf.ByteString.CopyFrom(destBytes),
                Amt = 1000,
                PaymentHash = Google.Protobuf.ByteString.CopyFrom(fakeHash),
                TimeoutSeconds = 30,
                FeeLimitSat = 50
            };

            using var stream = _routerClient.SendPaymentV2(request);
            await foreach (var update in stream.ResponseStream.ReadAllAsync())
            {
                if (update.Status == Payment.Types.PaymentStatus.Failed)
                {
                    // SUCCESS logic: If it failed because the hash was wrong, the node is UP.
                    bool isUp = update.FailureReason == PaymentFailureReason.FailureReasonIncorrectPaymentDetails;
                    return new ProbeResult(pubKey, isUp, (int)sw.ElapsedMilliseconds, DateTime.UtcNow);
                }
            }
        }
        catch (Exception ex)
        {
            return new ProbeResult(pubKey, false, 0, DateTime.UtcNow, ex.Message);
        }
        return new ProbeResult(pubKey, false, 0, DateTime.UtcNow, "Timeout");
    }
}
