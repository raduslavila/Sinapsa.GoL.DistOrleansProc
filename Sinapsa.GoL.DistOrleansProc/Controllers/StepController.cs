using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sinapsa.GoL.DistOrleansProc.Domain.Services;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using System.Text.Json;

namespace Sinapsa.GoL.DistOrleansProc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StepController : ControllerBase
    {
        private readonly ILogger<WarmUpController> _logger;
        private readonly IGoLService _goLService;

        public StepController(ILogger<WarmUpController> logger, IGoLService goLService)
        {
            _logger = logger;
            _goLService = goLService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            await _goLService.RunUniverseStep();

            var result = await _goLService.DisplayCurrentState();

            return result;
        }
    }
}
