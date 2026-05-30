using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models
{
    [GenerateSerializer]
    [Alias("Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models.Cell")]
    public class Cell
    {
        [Id(0)]
        public bool IsAlive;

        [Id(1)]
        public bool IsAliveNext;

        // Runtime-only graph links, intentionally excluded from Orleans serialization.
        [NonSerialized]
        //[Id(2)]
        public List<Cell> neighbors = new List<Cell>();
    }
}
