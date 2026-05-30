namespace Sinapsa.GoL.DistOrleansProc.Domain.Models
{
    public class UniverseGridUpdateDto
    {
        public int CurrentGeneration { get; set; }
        public bool IsFullGrid { get; set; }
        public GridStateDto? Grid { get; set; }
        public IReadOnlyCollection<UniverseGridCellDeltaDto> Deltas { get; set; } = Array.Empty<UniverseGridCellDeltaDto>();
    }
}
