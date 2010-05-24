/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Security.Cryptography;
using System.Text;
using System.Security;

namespace Lidgren.Network
{
	public sealed class NetXtea
	{
		private const int m_blockSize = 8;
		private const int m_keySize = 16;
		private const int m_delta = unchecked((int)0x9E3779B9);

		private readonly int m_numRounds;

		private uint[] m_sum0;
		private uint[] m_sum1;

		/// <summary>
		/// 16 byte key
		/// </summary>
		public NetXtea(byte[] key, int rounds)
		{
			if (key.Length < 16)
				throw new NetException("Key too short!");

			m_numRounds = rounds;
			m_sum0 = new uint[m_numRounds];
			m_sum1 = new uint[m_numRounds];
			uint[] tmp = new uint[8];

			int num2;
			int index = num2 = 0;
			while (index < 4)
			{
				tmp[index] = BitConverter.ToUInt32(key, num2);
				index++;
				num2 += 4;
			}
			for (index = num2 = 0; index < 32; index++)
			{
				m_sum0[index] = ((uint)num2) + tmp[num2 & 3];
				num2 += -1640531527;
				m_sum1[index] = ((uint)num2) + tmp[(num2 >> 11) & 3];
			}
		}

		/// <summary>
		/// 16 byte key
		/// </summary>
		public NetXtea(byte[] key)
			: this(key, 32)
		{
		}

		/// <summary>
		/// String to hash for key
		/// </summary>
		public NetXtea(string key)
			: this(NetSha.Hash(Encoding.ASCII.GetBytes(key)), 32)
		{
		}

		public void EncryptBlock(
			byte[] inBytes,
			int inOff,
			byte[] outBytes,
			int outOff)
		{
			uint v0 = BytesToUInt(inBytes, inOff);
			uint v1 = BytesToUInt(inBytes, inOff + 4);

			for (int i = 0; i != m_numRounds; i++)
			{
				v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ m_sum0[i];
				v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ m_sum1[i];
			}

			UIntToBytes(v0, outBytes, outOff);
			UIntToBytes(v1, outBytes, outOff + 4);

			return;
		}

		public void DecryptBlock(
			byte[] inBytes,
			int inOff,
			byte[] outBytes,
			int outOff)
		{
			// Pack bytes into integers
			uint v0 = BytesToUInt(inBytes, inOff);
			uint v1 = BytesToUInt(inBytes, inOff + 4);

			for (int i = m_numRounds - 1; i >= 0; i--)
			{
				v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ m_sum1[i];
				v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ m_sum0[i];
			}

			UIntToBytes(v0, outBytes, outOff);
			UIntToBytes(v1, outBytes, outOff + 4);

			return;
		}

		private static uint BytesToUInt(byte[] bytes, int offset)
		{
			uint retval = (uint)(bytes[offset] << 24);
			retval |= (uint)(bytes[++offset] << 16);
			retval |= (uint)(bytes[++offset] << 8);
			return (retval | bytes[++offset]);
		}

		private static void UIntToBytes(uint value, byte[] destination, int destinationOffset)
		{
			destination[destinationOffset++] = (byte)(value >> 24);
			destination[destinationOffset++] = (byte)(value >> 16);
			destination[destinationOffset++] = (byte)(value >> 8);
			destination[destinationOffset++] = (byte)value;
		}
	}

	public static class NetSha
	{
		// TODO: switch to SHA256
		private static SHA1 m_sha;

		public static byte[] Hash(byte[] data)
		{
			if (m_sha == null)
				m_sha = SHA1Managed.Create();
			return m_sha.ComputeHash(data);
		}
	}

	public static class NetSRP
	{
		private static readonly BigInteger N = new BigInteger(NetUtility.ToByteArray("0115b8b692e0e045692cf280b436735c77a5a9e8a9e7ed56c965f87db5b2a2ece3"));
		private static readonly BigInteger g = new BigInteger((uint)2);
		private static readonly BigInteger k = ComputeMultiplier();

		/// <summary>
		/// Compute multiplier (k)
		/// </summary>
		private static BigInteger ComputeMultiplier()
		{
			string one = NetUtility.ToHexString(N.GetBytes());
			string two = NetUtility.ToHexString(g.GetBytes());
			byte[] cc = NetUtility.ToByteArray(one + two.PadLeft(one.Length, '0'));
			return BigInteger.Modulus(new BigInteger(NetSha.Hash(cc)), N);
		}

		/// <summary>
		/// Creates a verifier that the server can use to authenticate users later on (v)
		/// </summary>
		public static byte[] ComputePasswordVerifier(string username, string password, byte[] salt)
		{
			byte[] tmp = Encoding.ASCII.GetBytes(username + ":" + password);
			byte[] innerHash = NetSha.Hash(tmp);

			byte[] total = new byte[innerHash.Length + salt.Length];
			Buffer.BlockCopy(salt, 0, total, 0, salt.Length);
			Buffer.BlockCopy(innerHash, 0, total, salt.Length, innerHash.Length);

			byte[] x = NetSha.Hash(total);

			// Verifier (v) = g^x (mod N) 
			BigInteger xx = new BigInteger(x);
			return g.ModPow(xx, N).GetBytes();
		}

		/// <summary>
		/// Get 256 random bits
		/// </summary>
		public static byte[] CreateRandomChallenge()
		{
			byte[] retval = new byte[32];
			NetRandom.Instance.NextBytes(retval);
			return retval;
		}

		/// <summary>
		/// Compute client challenge (A)
		/// </summary>
		public static byte[] ComputeClientChallenge(byte[] clientSalt) // a
		{
			BigInteger salt = new BigInteger(clientSalt);
			return g.ModPow(salt, N).GetBytes();
		}

		/// <summary>
		/// Compute server challenge (B)
		/// </summary>
		public static byte[] ComputeServerChallenge(byte[] serverSalt, byte[] verifier) // b
		{
			BigInteger salt = new BigInteger(serverSalt);

			var bb = g.ModPow(salt, N);
			var B = BigInteger.Modulus((bb + (new BigInteger(verifier) * k)), N);

			return B.GetBytes();
		}

		public static byte[] ComputeU(byte[] clientChallenge, byte[] serverChallenge)
		{
			byte[] A = clientChallenge;
			byte[] B = serverChallenge;

			string one = NetUtility.ToHexString(A);
			string two = NetUtility.ToHexString(B);
			string compound = one + two.PadLeft(one.Length, '0');
			byte[] cc = NetUtility.ToByteArray(compound);
			return NetSha.Hash(cc);
		}

		/*
		public static byte[] ComputeClientToken(byte[] serverChallenge, byte[] x, byte[] u


		// S = (B - kg^x) ^ (a + ux) (mod N)
function srp_compute_client_S(BB, xx, uu, aa, kk) {
  var bx = g.modPow(xx, N);
  var btmp = BB.add(N.multiply(kk)).subtract(bx.multiply(kk)).mod(N);
  return btmp.modPow(xx.multiply(uu).add(aa), N);
}
		*/
 
		public static byte[] ComputeServerToken(byte[] clientChallenge, byte[] verifier, byte[] u, byte[] serverChallengeSalt)
		{
			// S = (Av^u) ^ b (mod N)
			// function srp_compute_server_S(AA, vv, uu, bb) {

			BigInteger vv = new BigInteger(verifier);

			BigInteger c1 = vv.ModPow(new BigInteger(u), N);
			BigInteger c2 = new BigInteger(clientChallenge);

			BigInteger r1 = c1 * c2;

			BigInteger r2 = BigInteger.Modulus(r1, N);

			return r2.ModPow(new BigInteger(serverChallengeSalt), N).GetBytes();
			//return vv.modPow(uu, N).multiply(A).mod(N).modPow(bb, N);
		}
	}
}
