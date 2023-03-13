using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Configuration
{
    public class ClusterConfig
    {
        public bool UseLocalhost { get; set; }
        public bool UseDashboard { get; set; }
        public int? DashboardPort { get; set; }
        public string DashboardPath { get; set; }
        public bool UseLinuxStatistics { get; set; }
        public ClusterOptions ClusterOptions { get; set; } = new ClusterOptions();
        public ClusterEndpointOptions ClusterEndpointOptions { get; set; } = new ClusterEndpointOptions();
        public bool UseKubernetesHosting { get; set; }
    }
}
