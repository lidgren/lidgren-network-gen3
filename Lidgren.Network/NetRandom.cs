using System;
using System.Collections.Generic;
using System.Threading;

namespace Lidgren.Network
{
	/// <summary>
	/// Mersenne Twister PRNG
	/// </summary>
	public sealed class NetRandom
	{
		/// <summary>
		/// Gets a global NetRandom instance
		/// </summary>
		public static readonly NetRandom Instance = new NetRandom();

		private const double c_uniformSingleMultiplier = 1.0 / ((double)uint.MaxValue + 1.0);

		private static int m_seedIncrement = 997;

		private const int N = 624;
		private const int M = 397;
		private const uint MATRIX_A = 0x9908b0dfU;
		private const uint UPPER_MASK = 0x80000000U;
		private const uint LOWER_MASK = 0x7fffffffU;
		private const uint TEMPER1 = 0x9d2c5680U;
		private const uint TEMPER2 = 0xefc60000U;
		private const int TEMPER3 = 11;
		private const int TEMPER4 = 7;
		private const int TEMPER5 = 15;
		private const int TEMPER6 = 18;

		private UInt32[] mt;
		private int mti;
		private UInt32[] mag01;

		/// <summary>
		/// Constructor
		/// </summary>
		public NetRandom()
		{
			// make seed from various numbers
			uint seed = NetHash.Hash(
				(int)Environment.TickCount,
				Guid.NewGuid().GetHashCode(),
				this.GetHashCode(),
				m_seedIncrement
				// can't use Environment.WorkingSet or Stopwatch.GetTimestamp here since it's not available or reliable on all platforms
			);

			mt = new UInt32[N];
			mti = N + 1;
			mag01 = new UInt32[] { 0x0U, MATRIX_A };
			mt[0] = seed;
			for (int i = 1; i < N; i++)
				mt[i] = (UInt32)(1812433253 * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i);
		}

		/// <summary>
		/// Generates a random value from Int32.MinValue to Int32.MaxValue
		/// </summary>
		[CLSCompliant(false)]
		public uint NextUInt32()
		{
			UInt32 y;
			if (mti >= N)
			{
				GenRandAll();
				mti = 0;
			}
			y = mt[mti++];
			y ^= (y >> TEMPER3);
			y ^= (y << TEMPER4) & TEMPER1;
			y ^= (y << TEMPER5) & TEMPER2;
			y ^= (y >> TEMPER6);
			return y;
		}

		private void GenRandAll()
		{
			int kk = 1;
			UInt32 y;
			UInt32 p;
			y = mt[0] & UPPER_MASK;
			do
			{
				p = mt[kk];
				mt[kk - 1] = mt[kk + (M - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
				y = p & UPPER_MASK;
			} while (++kk < N - M + 1);
			do
			{
				p = mt[kk];
				mt[kk - 1] = mt[kk + (M - N - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
				y = p & UPPER_MASK;
			} while (++kk < N);
			p = mt[0];
			mt[N - 1] = mt[M - 1] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
		}

		/// <summary>
		/// Fills all bytes in the provided buffer with random values
		/// </summary>
		public void NextBytes(byte[] buffer)
		{
			NextBytes(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Fills all bytes from offset to offset + length in buffer with random values
		/// </summary>
		public void NextBytes(byte[] buffer, int offset, int length)
		{
			int full = length / 4;
			int ptr = offset;
			for (int i = 0; i < full; i++)
			{
				uint r = NextUInt32();
				buffer[ptr++] = (byte)r;
				buffer[ptr++] = (byte)(r >> 8);
				buffer[ptr++] = (byte)(r >> 16);
				buffer[ptr++] = (byte)(r >> 24);
			}

			int rest = length - (full * 4);
			for (int i = 0; i < rest; i++)
				buffer[ptr++] = (byte)NextUInt32();
		}

		/// <summary>
		/// Returns a random value >= 0.0f and < 1.0f
		/// </summary>
		public float NextSingle()
		{
			return (float)((double)NextUInt32() * c_uniformSingleMultiplier);
		}

		/// <summary>
		/// Returns random value that is >= 0.0 and < 1.0
		/// </summary>
		public double NextDouble()
		{
			return (double)NextUInt32() * c_uniformSingleMultiplier;
		}
	}
}
