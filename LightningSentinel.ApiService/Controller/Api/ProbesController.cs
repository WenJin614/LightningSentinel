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
        public async Task<IActionResult> ReceiveProbeAsync([FromBody] ProbeResult result)
        {
            await _probeService.AddProbeResult(result);
            return Ok();
        }
    }
}
