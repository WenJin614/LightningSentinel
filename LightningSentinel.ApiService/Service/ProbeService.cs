using LightningSentinel.Data.Entities;
using LightningSentinel.Shared.LightningProbe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightningSentinel.ApiService.Service
{
    public class ProbeService : IProbeService
    {
        private readonly SentinelDbContext _context;
        private readonly ILogger _logger;

        public ProbeService(SentinelDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddProbeResult(ProbeResult result, CancellationToken ct)
        {
            try
            {
                var entity = new ProbeResultEntity
                {
                    PubKey = result.PubKey,
                    IsAlive = result.IsAlive,
                    LatencyMs = result.LatencyMs,
                    CheckedAt = result.CheckedAt,
                };

                _context.Set<ProbeResultEntity>().Add(entity);
                await _context.SaveChangesAsync(ct);

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Database operation was cancelled by the user or system.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Actual database error while saving probe result");
                return false;
            }
        }
    }
}
