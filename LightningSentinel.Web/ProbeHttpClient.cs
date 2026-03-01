// Blazor Project / Services / ProbeHttpClient.cs
using System.Net.Http.Json;
using LightningSentinel.Data.Entities; // Ensure shared entities are referenced

public class ProbeHttpClient
{
    private readonly HttpClient _http;

    public ProbeHttpClient(HttpClient http)
    {
        _http = http;
    }
    public async Task<HealthResponse?> GetHealthAsync(string pubKey, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(pubKey)) return null;

        // 1. Encode the PubKey (Safety first!)
        var encodedPubKey = Uri.EscapeDataString(pubKey);

        try
        {
            // 2. Ensure NO leading slash in the string
            // The HttpClient BaseAddress + "api/v1/..." combines them correctly.
            var requestUri = $"api/v1/probes/{encodedPubKey}?limit={limit}";

            return await _http.GetFromJsonAsync<HealthResponse>(requestUri);
        }
        catch (HttpRequestException ex)
        {
            // Log the actual URL that failed to help you debug
            Console.WriteLine($"Request failed: {_http.BaseAddress}{pubKey}. Status: {ex.StatusCode}");
            throw;
        }
    }

    public class HealthResponse
    {
        public string PubKey { get; set; } = "";
        public string ReliabilityScore { get; set; } = "";
        public List<ProbeResultEntity> Data { get; set; } = new();
    }
}