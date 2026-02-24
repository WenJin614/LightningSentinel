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
[HttpPost]
public async Task<IActionResult> ReceiveProbeAsync([FromBody] ProbeResult result, CancellationToken ct)
{
    // 1. Basic validation
    if (result == null)
    {
        return BadRequest("Probe result cannot be null.");
    }

    // 2. Call the service and check the boolean result
    bool isSuccess = await _probeService.AddProbeResult(result, ct);

    if (!isSuccess)
    {
        // 3. Return a 500 status code if the service failed
        return StatusCode(500, "A database error occurred while saving the probe result.");
    }

    // 4. Return 200 OK only if it actually saved
    return Ok(new { message = "Probe result saved successfully." });
}
    }
}
