using LightningSentinel.Shared.LightningProbe;
using Microsoft.AspNetCore.Mvc;

namespace LightningSentinel.ApiService.Controller.Api
{
    [ApiController]
    [Route("api/v1/[controller]")] // This maps to api/v1/probes
    public class ProbesController : ControllerBase
    {
        [HttpPost]
        public IActionResult ReceiveProbe([FromBody] ProbeResult result)
        {
            // 1. Process the data (e.g., save to a database)
            Console.WriteLine($"Received probe from: {result.PubKey}");

            // 2. Return a 201 Created or 202 Accepted
            return Accepted();
        }
    }
}
