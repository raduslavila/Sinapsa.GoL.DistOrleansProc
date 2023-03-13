using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public long UnixTimestampNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}