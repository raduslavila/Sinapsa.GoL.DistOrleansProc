using Orleans;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    /// <summary>Full grid snapshot returned by the universe grain.</summary>
    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.UniverseStateDto")]
    public class UniverseStateDto
    {
        [Id(0)]
        public int Width { get; set; }

        [Id(1)]
        public int Height { get; set; }

        [Id(2)]
        public int Generation { get; set; }

        /// <summary>cells[x][y] = true means alive.</summary>
        [Id(3)]
        public bool[][] Cells { get; set; } = Array.Empty<bool[]>();
    }

    /// <summary>Delta update returned by the universe grain.</summary>
    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.UniverseUpdateDto")]
    public class UniverseUpdateDto
    {
        [Id(0)]
        public int CurrentGeneration { get; set; }

        [Id(1)]
        public bool IsFullGrid { get; set; }

        /// <summary>Populated when <see cref="IsFullGrid"/> is true.</summary>
        [Id(2)]
        public UniverseStateDto? Grid { get; set; }

        /// <summary>Populated when <see cref="IsFullGrid"/> is false.</summary>
        [Id(3)]
        public List<UniverseCellDeltaDto> Deltas { get; set; } = new();
    }

    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.UniverseCellDeltaDto")]
    public class UniverseCellDeltaDto
    {
        [Id(0)]
        public int X { get; set; }

        [Id(1)]
        public int Y { get; set; }

        [Id(2)]
        public bool IsAlive { get; set; }
    }
}
