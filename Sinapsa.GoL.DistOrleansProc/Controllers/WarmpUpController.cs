using Microsoft.AspNetCore.Mvc;

namespace Sinapsa.GoL.DistOrleansProc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WarmpUpController : ControllerBase
    {
        private readonly ILogger<WarmpUpController> _logger;

        public WarmpUpController(ILogger<WarmpUpController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Working");
        }
    }
}