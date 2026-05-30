using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    public interface IGoLService
    {
        Task InitUniverse(int chunksX, int chunksY, int chunkSize, double liveDensity);
        Task RunUniverseStep();
        Task<string> DisplayUniverseState();
    }
}