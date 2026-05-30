using Orleans;
using Sinapsa.GoL.DistOrleansProc.Domain.Models;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces;
using System.Text;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    /// <summary>
    /// Thin proxy: all mutable universe state now lives in <see cref="IGoLUniverseGrain"/>
    /// (a cluster-singleton grain with key "universe"), eliminating the per-silo inconsistency
    /// that caused different generation numbers when HTTP requests hit different pods.
    /// </summary>
    public class GoLService : IGoLService
    {
        private readonly IGrainFactory _grainFactory;

        public GoLService(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        private IGoLUniverseGrain Universe =>
            _grainFactory.GetGrain<IGoLUniverseGrain>("universe");

        public Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity) =>
            Universe.InitUniverse(chunksX, chunksY, chunkSize, liveDensity);

        public Task ClearAndReinitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity) =>
            Universe.ClearAndReinitUniverse(chunksX, chunksY, chunkSize, liveDensity);

        public Task RunUniverseStep() => Universe.StepUniverse();

        public async Task<GridStateDto> GetUniverseGrid()
        {
            var state = await Universe.GetState();
            return new GridStateDto
            {
                Width  = state.Width,
                Height = state.Height,
                Cells  = state.Cells
            };
        }

        public async Task<UniverseGridUpdateDto> GetUniverseUpdate(int lastSeenGeneration)
        {
            var update = await Universe.GetUpdate(lastSeenGeneration);
            return new UniverseGridUpdateDto
            {
                CurrentGeneration = update.CurrentGeneration,
                IsFullGrid        = update.IsFullGrid,
                Grid              = update.Grid == null ? null : new GridStateDto
                {
                    Width  = update.Grid.Width,
                    Height = update.Grid.Height,
                    Cells  = update.Grid.Cells
                },
                Deltas = update.Deltas
                    .Select(d => new UniverseGridCellDeltaDto { X = d.X, Y = d.Y, IsAlive = d.IsAlive })
                    .ToArray()
            };
        }
    }
}
