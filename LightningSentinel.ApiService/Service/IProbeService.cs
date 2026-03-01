using LightningSentinel.Data.Entities;
using LightningSentinel.Shared.LightningProbe;

namespace LightningSentinel.ApiService.Service
{
    public interface IProbeService
    {
        Task<bool> AddProbeResult(ProbeResult result, CancellationToken ct);
        Task<List<ProbeResultEntity>> GetRecentProbes(string pubKey, int limit = 50);
    }
}
