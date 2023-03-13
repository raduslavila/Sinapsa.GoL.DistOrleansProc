using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Services
{
    public struct DefaultIdentity : IEquatable<DefaultIdentity>
    {
        public static readonly DefaultIdentity Default;

		public bool Equals(DefaultIdentity _)
		{
			return true;
		}

		public override bool Equals(object obj)
		{
			return obj is DefaultIdentity;
		}

		public override int GetHashCode()
		{
			return 0;
		}
	}
}
