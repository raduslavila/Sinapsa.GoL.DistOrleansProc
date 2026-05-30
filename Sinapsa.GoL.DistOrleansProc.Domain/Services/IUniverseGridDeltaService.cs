using Sinapsa.GoL.DistOrleansProc.Domain.Models;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    public interface IUniverseGridDeltaService
    {
        UniverseGridUpdateDto BuildUpdate(int lastSeenGeneration, int currentGeneration, GridStateDto currentGrid, GridStateDto? previousGrid);
    }
}
