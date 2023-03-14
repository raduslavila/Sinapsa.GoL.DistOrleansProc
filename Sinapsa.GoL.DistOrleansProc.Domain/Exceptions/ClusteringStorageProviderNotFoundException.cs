using Sinapsa.GoL.DistOrleansProc.Domain.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Exceptions
{
    public class ClusteringStorageProviderNotFoundException : Exception
    {
        public ClusteringStorageProviderNotFoundException() : base($"{nameof(ClusterConfig.UseLocalhost)} not found. Please set the clustering storage provider type!")
        { }
    }
}
