using System;

namespace Lidgren.Network
{
	/// <summary>
	/// Murmur2 hash code
	/// </summary>
	public static class NetHash
	{
		/// <summary>
		/// Hash values into a single UInt32
		/// </summary>
		[CLSCompliant(false)]
		public static uint Hash(params int[] data)
		{
			unchecked
			{
				const uint m = 0x5bd1e995;
				const int r = 24;

				UInt32 h = 0xc58f1a7b ^ (uint)data.Length;
				for (int i = 0; i < data.Length; i++)
				{
					var k = (uint)data[i] * m;
					k ^= k >> r; k *= m;
					h *= m; h ^= k;
				}

				// final mix
				h ^= h >> 13; h *= m; h ^= h >> 15;

				return h;
			}
		}
	}
}
