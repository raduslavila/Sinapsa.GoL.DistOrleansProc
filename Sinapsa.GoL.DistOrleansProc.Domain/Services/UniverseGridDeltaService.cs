using Sinapsa.GoL.DistOrleansProc.Domain.Models;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Services
{
    public class UniverseGridDeltaService : IUniverseGridDeltaService
    {
        public UniverseGridUpdateDto BuildUpdate(int lastSeenGeneration, int currentGeneration, GridStateDto currentGrid, GridStateDto? previousGrid)
        {
            var response = new UniverseGridUpdateDto
            {
                CurrentGeneration = currentGeneration
            };

            var requiresFullGrid = previousGrid == null
                                   || lastSeenGeneration < 0
                                   || lastSeenGeneration > currentGeneration
                                   || currentGeneration - lastSeenGeneration > 1
                                   || previousGrid.Width != currentGrid.Width
                                   || previousGrid.Height != currentGrid.Height;

            if (requiresFullGrid)
            {
                response.IsFullGrid = true;
                response.Grid = currentGrid;
                return response;
            }

            if (lastSeenGeneration == currentGeneration)
            {
                response.IsFullGrid = false;
                response.Deltas = Array.Empty<UniverseGridCellDeltaDto>();
                return response;
            }

            var deltas = new List<UniverseGridCellDeltaDto>();
            for (int x = 0; x < currentGrid.Width; x++)
            {
                for (int y = 0; y < currentGrid.Height; y++)
                {
                    if (previousGrid.Cells[x][y] != currentGrid.Cells[x][y])
                    {
                        deltas.Add(new UniverseGridCellDeltaDto
                        {
                            X = x,
                            Y = y,
                            IsAlive = currentGrid.Cells[x][y]
                        });
                    }
                }
            }

            response.IsFullGrid = false;
            response.Deltas = deltas;
            return response;
        }
    }
}
