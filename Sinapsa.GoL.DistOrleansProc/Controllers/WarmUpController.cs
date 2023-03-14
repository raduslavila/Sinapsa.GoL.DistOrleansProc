using Microsoft.AspNetCore.Mvc;
using Sinapsa.GoL.DistOrleansProc.Domain.Services;

namespace Sinapsa.GoL.DistOrleansProc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WarmUpController : ControllerBase
    {
        private readonly ILogger<WarmUpController> _logger;
        private readonly IGoLService _goLService;

        public WarmUpController(ILogger<WarmUpController> logger, IGoLService goLService)
        {
            _logger = logger;
            _goLService = goLService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _goLService.InitChunk();

            return Ok("Initialized Game of Life");
        }
    }
}