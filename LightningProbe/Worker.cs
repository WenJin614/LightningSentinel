using System.Net.Http.Json;
using LightningSentinel.Shared.LightningProbe;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;

    public Worker(ILogger<Worker> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting probe cycle at: {time}", DateTimeOffset.Now);

            // 1. Define the target (Example: Kraken's PubKey)
            string targetNode = "02ee...krakenPubKey";

            // 2. Perform the "Fake Payment" Probe
            var result = await PerformProbe(targetNode);

            // 3. Send the result to the Sentinel API
            // Note: "apiservice" is the name given in Aspire AppHost
            await _httpClient.PostAsJsonAsync("http://apiservice/api/v1/probes", result, stoppingToken);

            _logger.LogInformation("Probe sent to Sentinel. Status: {status}", result.IsAlive);

            // Wait 5 minutes before the next check
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task<ProbeResult> PerformProbe(string pubKey)
    {
        // LOGIC: To probe without losing money, we send a payment with a 
        // completely RANDOM hash. The destination node will receive it, 
        // see that it doesn't have the preimage, and return an "Incorrect Payment Details" error.
        // IF we get that error, it means the path is ALIVE.

        try
        {
            // [Mocking the gRPC call to LND for this example]
            // In reality, you'd use: lndClient.Router.SendPaymentV2(...)
            bool success = true;
            int latency = new Random().Next(50, 500);

            return new ProbeResult(pubKey, success, latency, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new ProbeResult(pubKey, false, 0, DateTime.UtcNow, ex.Message);
        }
    }
}
