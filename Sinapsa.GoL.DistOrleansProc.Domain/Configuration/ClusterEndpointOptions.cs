using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sinapsa.GoL.DistOrleansProc.Domain.Configuration
{
    public class ClusterEndpointOptions
    {
        public int SiloPort { get; set; }
        public int GatewayPort { get; set; }
        public string AdvertisedIP { get; set; }
        public string SiloListeningIP { get; set; }
        public string GatewayListeningIP { get; set; }
        public IPAddress AdvertisedIPAddress
        {
            get
            {
                return !string.IsNullOrEmpty(AdvertisedIP) ? IPAddress.Parse(AdvertisedIP) : IPAddress.Loopback;
            }
        }
        public IPEndPoint SiloListeningEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(SiloListeningIP))
                {
                    return new IPEndPoint(IPAddress.Parse(SiloListeningIP), SiloPort);
                }
                return null;
            }
        }
        public IPEndPoint GatewayListeningEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(GatewayListeningIP))
                {
                    return new IPEndPoint(IPAddress.Parse(GatewayListeningIP), GatewayPort);
                }
                return null;
            }
        }
    }
}
