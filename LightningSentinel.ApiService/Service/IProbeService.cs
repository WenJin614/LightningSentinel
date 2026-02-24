using LightningSentinel.Shared.LightningProbe;

namespace LightningSentinel.ApiService.Service
{
    public interface IProbeService
    {
        Task<bool> AddProbeResult(ProbeResult result, CancellationToken ct);
    }
}
