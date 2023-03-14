using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    public interface IGoLService
    {
        Task<string> DisplayCurrentState();
        Task<bool[,]> GetCurrentState();
        void InitChunk();
        Task RunUniverseStep();
    }
}