using Orleans;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.GoLUniverseGrainState")]
    public class GoLUniverseGrainState
    {
        [Id(0)]
        public int ChunksX { get; set; }

        [Id(1)]
        public int ChunksY { get; set; }

        [Id(2)]
        public int ChunkSize { get; set; }

        [Id(3)]
        public int CurrentGeneration { get; set; }

        /// <summary>Whether the universe has been initialised at least once.</summary>
        [Id(4)]
        public bool IsInitialized { get; set; }
    }
}
