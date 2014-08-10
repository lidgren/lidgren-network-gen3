using System;
using System.Threading;

namespace Lidgren.Network
{
	public static class NetRandomSeed
	{
		private static int m_seedIncrement = -1640531527;

		/// <summary>
		/// Generates a 32 bit random seed
		/// </summary>
		[CLSCompliant(false)]
		public static uint GetUInt32()
		{
			ulong seed = GetUInt64();
			uint low = (uint)seed;
			uint high = (uint)(seed >> 32);
			return low ^ high;
		}

		/// <summary>
		/// Generates a 64 bit random seed
		/// </summary>
		[CLSCompliant(false)]
		public static ulong GetUInt64()
		{
#if !__ANDROID__ && !IOS && !UNITY_WEBPLAYER
			ulong seed = (ulong)System.Diagnostics.Stopwatch.GetTimestamp();
			seed ^= (ulong)Environment.WorkingSet;
			ulong s2 = (ulong)Interlocked.Increment(ref m_seedIncrement);
			s2 |= (((ulong)Guid.NewGuid().GetHashCode()) << 32);
			seed ^= s2;
#else
			ulong v1 = (ulong)Environment.TickCount;
			v1 |= (((ulong)(new object().GetHashCode())) << 32);
			ulong v2 = (ulong)Guid.NewGuid().GetHashCode();
			v2 |= (((ulong)(Interlocked.Increment(ref m_seedIncrement)) << 32);
			return v1 ^ v2;
#endif
			return seed;
		}
	}
}
