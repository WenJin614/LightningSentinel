using LightningSentinel.Shared.LightningProbe;
using Grpc.Core;
using Sentinel.Grpc;

namespace LightningSentinel.ApiService.Service.Grpc
{
    public class GrpcProbeService : ProbeGrpcService.ProbeGrpcServiceBase
    {
        private readonly IProbeService _dbService;

        public GrpcProbeService(IProbeService dbService)
        {
            _dbService = dbService;
        }

        public override async Task<ProbeResponse> SendProbeResult(ProbeRequest request, ServerCallContext context)
        {
            var checkedAt = DateTimeOffset.FromUnixTimeSeconds(request.CheckedAtUnix).UtcDateTime;

            var result = new ProbeResult(
                request.PubKey,
                request.IsAlive,
                request.LatencyMs,
                checkedAt
            );

            bool success = await _dbService.AddProbeResult(result, context.CancellationToken);
            return new ProbeResponse { Success = success };
        }
    }
}
