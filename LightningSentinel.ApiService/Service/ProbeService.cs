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

        public async Task<bool> AddProbeResult(ProbeResult result)
        {
            try
            {
                //_context.ProbeResults.Add(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error");
                return false;
            }
        }
    }
}
