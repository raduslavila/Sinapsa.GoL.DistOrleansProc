using Orleans;
using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
using Sinapsa.GoL.DistOrleansProc.Orleans.Core;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces
{
    /// <summary>
    /// Singleton coordinator grain (key = "universe") that owns all universe-level state:
    /// chunk dimensions, current generation and grid snapshots.
    /// Because a grain with a fixed key always lives on exactly one silo, its in-memory
    /// state is cluster-unique — eliminating the multi-silo inconsistency that existed when
    /// GoLService (a per-silo DI singleton) held this state.
    /// </summary>
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.IGoLUniverseGrain")]
    public interface IGoLUniverseGrain : IGrainWithStringKey, IGrain<string>
    {
        Task<bool> IsInitialized();

        Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity);

        Task ClearAndReinitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity);

        Task StepUniverse();

        Task<UniverseStateDto> GetState();

        Task<UniverseUpdateDto> GetUpdate(int lastSeenGeneration);
    }
}
