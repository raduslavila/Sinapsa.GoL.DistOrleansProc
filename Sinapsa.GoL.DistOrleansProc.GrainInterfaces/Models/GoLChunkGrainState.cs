using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.GoLChunkGrainState")]
    public class GoLChunkGrainState
    {
        [Id(0)]
        public string? ChunkId { get; set; }

        [Id(1)]
        public int ChunkLocationX { get; set; }

        [Id(2)]
        public int ChunkLocationY { get; set; }

        [Id(3)]
        public Cell[,]? Cells;

        [Id(4)]
        public int Size { get; set; } // Chunks are always square (Size x Size)

        // Neighbor chunk identifiers for inter-chunk communication
        [Id(5)]
        public string? TopChunkId { get; set; }

        [Id(6)]
        public string? BottomChunkId { get; set; }

        [Id(7)]
        public string? LeftChunkId { get; set; }

        [Id(8)]
        public string? RightChunkId { get; set; }

        [Id(9)]
        public string? TopLeftChunkId { get; set; }

        [Id(10)]
        public string? TopRightChunkId { get; set; }

        [Id(11)]
        public string? BottomLeftChunkId { get; set; }

        [Id(12)]
        public string? BottomRightChunkId { get; set; }
    }
}
