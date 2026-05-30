using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Sinapsa.GoL.DistOrleansProc.Domain.Services;

namespace Sinapsa.GoL.DistOrleansProc.Controllers
{
    [ApiController]
    [Route("api")]
    public class UniverseController : ControllerBase
    {
        private readonly ILogger<UniverseController> _logger;
        private readonly IGoLService _goLService;

        public UniverseController(ILogger<UniverseController> logger, IGoLService goLService)
        {
            _logger = logger;
            _goLService = goLService;
        }

        /// <summary>
        /// Initialize a distributed universe with multiple chunks
        /// </summary>
        /// <param name="chunksX">Number of chunks horizontally (default: 2)</param>
        /// <param name="chunksY">Number of chunks vertically (default: 2)</param>
        /// <param name="chunkSize">Size of each square chunk (default: 32 = 32x32)</param>
        /// <param name="liveDensity">Initial percentage of live cells (default: 0.15)</param>
        [HttpPost("init")]
        public async Task<IActionResult> InitUniverse(
            [FromQuery] int chunksX = 2,
            [FromQuery] int chunksY = 2,
            [FromQuery] int chunkSize = 32,
            [FromQuery] double liveDensity = 0.15)
        {
            _logger.LogInformation(
                "Initializing distributed universe: {ChunksX}x{ChunksY} chunks, each {ChunkSize}x{ChunkSize} cells",
                chunksX, chunksY, chunkSize, chunkSize);

            await _goLService.InitUniverse(chunksX, chunksY, chunkSize, liveDensity);

            return Ok(new
            {
                message = "Distributed universe initialized",
                configuration = new
                {
                    chunksX,
                    chunksY,
                    chunkSize,
                    totalWidth = chunksX * chunkSize,
                    totalHeight = chunksY * chunkSize,
                    totalCells = chunksX * chunkSize * chunksY * chunkSize,
                    totalChunks = chunksX * chunksY,
                    liveDensity
                }
            });
        }

        /// <summary>
        /// Advance the distributed universe by one generation
        /// All chunks will process their cells in parallel and communicate edge states
        /// </summary>
        [HttpPost("step")]
        public async Task<ActionResult<string>> RunStep()
        {
            _logger.LogInformation("Running distributed universe step");

            await _goLService.RunUniverseStep();

            var state = await _goLService.DisplayUniverseState();

            return Ok(state);
        }

        /// <summary>
        /// Get the current state of the entire distributed universe
        /// </summary>
        [HttpGet("state")]
        public async Task<ActionResult<string>> GetState()
        {
            var state = await _goLService.DisplayUniverseState();
            return Ok(state);
        }
    }
}
