using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    public class GoLChunkGrainState
    {
        public string? ChunkId { get; set; }
        public int ChunkLocationX { get; set; }
        public int ChunkLocationy { get; set; }

        public Cell[,]? Cells;
        public int Width { get; set; }
        public int Height { get; set; }

        // Neighbor chunk identifiers for inter-chunk communication
        public string? TopChunkId { get; set; }
        public string? BottomChunkId { get; set; }
        public string? LeftChunkId { get; set; }
        public string? RightChunkId { get; set; }
        public string? TopLeftChunkId { get; set; }
        public string? TopRightChunkId { get; set; }
        public string? BottomLeftChunkId { get; set; }
        public string? BottomRightChunkId { get; set; }
    }
}
