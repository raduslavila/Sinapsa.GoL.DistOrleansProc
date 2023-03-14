using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    public class GoLChunkGrainState
    {
        public string ChunkId { get; set; }

        public Cell[,] Cells;
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
