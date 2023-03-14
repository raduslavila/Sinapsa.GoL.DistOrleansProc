using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Configuration
{
    public class ClientConfig
    {
        public const string DefaultClusteringStorageCredential = "Default";
        public bool UseLocalhost { get; set; }
        public string ClusteringStorageCredential { get; set; } = DefaultClusteringStorageCredential;
        public bool UseKubernetesHosting { get; set; }
        public ClusterOptions ClusterOptions { get; set; } = new ClusterOptions();
    }
}
