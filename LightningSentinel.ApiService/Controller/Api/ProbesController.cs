using LightningSentinel.ApiService.Service;
using LightningSentinel.Shared.LightningProbe;
using Microsoft.AspNetCore.Mvc;

namespace LightningSentinel.ApiService.Controller.Api
{
    [ApiController]
    [Route("api/v1/[controller]")] // This maps to api/v1/probes
    public class ProbesController : ControllerBase
    {
        private readonly IProbeService _probeService;

        public ProbesController(IProbeService probeService)
        {
            _probeService = probeService;
        }

        /// <summary>
        /// Store probe result into db.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [HttpGet("{pubKey}")]
        public async Task<IActionResult> GetProbeHistory(string pubKey, [FromQuery] int limit = 50)
        {
            var results = await _probeService.GetRecentProbes(pubKey, limit);

            if (results == null || !results.Any())
            {
                return NotFound(new { message = $"No probe data found for {pubKey}" });
            }

            // Calculate a quick "Health Score" for the buyer
            var upCount = results.Count(r => r.IsAlive);
            var reliability = (double)upCount / results.Count * 100;

            return Ok(new
            {
                pubKey,
                reliabilityScore = $"{reliability}%",
                data = results
            });
        }
    }
}
