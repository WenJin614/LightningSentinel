namespace LightningSentinel.Shared.LightningProbe
{
    public record ProbeResult(
        string PubKey,
        bool IsAlive,
        int LatencyMs,
        DateTime CheckedAt,
        string? ErrorMessage = null
    );
}
