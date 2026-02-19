using Grpc.Core;
using LightningSentinel.Shared;
using LightningSentinel.Shared.LightningProbe;
using Lnrpc;
using Microsoft.Extensions.Options;
using Routerrpc;
using System.Net.Http.Json;
using System.Runtime;
using System.Security.Cryptography;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly LightningSettings _lightningSettings;

    // NATIVE gRPC CLIENTS
    private readonly Lnrpc.Lightning.LightningClient _lndClient;
    private readonly Routerrpc.Router.RouterClient _routerClient;

    public Worker(
            ILogger<Worker> logger,
            HttpClient httpClient,
            IOptions<LightningSettings> lightningSettings,
            Lnrpc.Lightning.LightningClient lndClient,      // Injected from Program.cs
            Routerrpc.Router.RouterClient routerClient)      // Injected from Program.cs
    {
        _logger = logger;
        _httpClient = httpClient;
        _lightningSettings = lightningSettings.Value;
        _lndClient = lndClient;
        _routerClient = routerClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting probe cycle at: {time}", DateTimeOffset.Now);

            // 1. Define the target (Example: Kraken's PubKey)
            string targetNode = _lightningSettings.KarakenNode;

            // 2. Perform the "Fake Payment" Probe
            var result = await SentinelCheck(targetNode, deepProbe: false);

            // 3. Send the result to the Sentinel API
            // Note: "apiservice" is the name given in Aspire AppHost
            await _httpClient.PostAsJsonAsync("http://apiservice/api/v1/probes", result, stoppingToken);

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
