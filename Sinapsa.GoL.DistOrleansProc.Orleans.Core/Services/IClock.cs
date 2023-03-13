using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
        long UnixTimestampNow { get; }
    }
}