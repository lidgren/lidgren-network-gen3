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
		public static readonly BigInteger N = new BigInteger(NetUtility.ToByteArray("0115b8b692e0e045692cf280b436735c77a5a9e8a9e7ed56c965f87db5b2a2ece3"));
		public static readonly BigInteger g = new BigInteger((uint)2);
		public static readonly BigInteger k = ComputeMultiplier();

		/// <summary>
		/// Compute multiplier (k)
		/// </summary>
		private static BigInteger ComputeMultiplier()
		{
			string one = NetUtility.ToHexString(N.GetBytes());
			string two = NetUtility.ToHexString(g.GetBytes());
			byte[] cc = NetUtility.ToByteArray(one + two.PadLeft(one.Length, '0'));
			BigInteger retval = BigInteger.Modulus(new BigInteger(NetSha.Hash(cc)), N);
			return retval;
		}

		/// <summary>
		/// Creates a verifier that the server can use to authenticate users later on (v)
		/// </summary>
		public static void ComputePasswordVerifier(string username, string password, byte[] salt, out byte[] serverVerifier, out byte[] clientVerifier)
		{
			byte[] tmp = Encoding.ASCII.GetBytes(username + ":" + password);
			byte[] innerHash = NetSha.Hash(tmp);

			byte[] total = new byte[innerHash.Length + salt.Length];
			Buffer.BlockCopy(salt, 0, total, 0, salt.Length);
			Buffer.BlockCopy(innerHash, 0, total, salt.Length, innerHash.Length);

			clientVerifier = NetSha.Hash(total);

			// Verifier (v) = g^x (mod N) 
			BigInteger xx = new BigInteger(clientVerifier);
			serverVerifier = g.ModPow(xx, N).GetBytes();

			return;
		}

		/// <summary>
		/// Get 256 random bits
		/// </summary>
		public static byte[] CreateRandomKey()
		{
			byte[] retval = new byte[32];
			NetRandom.Instance.NextBytes(retval);
			return retval;
		}

		/// <summary>
		/// Gets 80 random bits
		/// </summary>
		public static byte[] CreateRandomSalt()
		{
			byte[] retval = new byte[10];
			NetRandom.Instance.NextBytes(retval);
			return retval;
		}

		/// <summary>
		/// Compute client challenge (A)
		/// </summary>
		public static byte[] ComputeClientPublicKey(byte[] clientPrivateKey) // a
		{
			BigInteger salt = new BigInteger(clientPrivateKey);

			BigInteger retval = g.ModPow(salt, N);

			string gs = NetUtility.ToHexString(g.GetBytes());


			Console.WriteLine("SALT: " + NetUtility.ToHexString(salt.GetBytes()));
			Console.WriteLine("A: " + NetUtility.ToHexString(retval.GetBytes()));

			return retval.GetBytes();
		}

		/// <summary>
		/// Compute server challenge (B)
		/// </summary>
		public static byte[] ComputeServerPublicKey(byte[] serverPrivateKey, byte[] verifier) // b
		{
			BigInteger salt = new BigInteger(serverPrivateKey);

			var bb = g.ModPow(salt, N);
			var B = BigInteger.Modulus((bb + (new BigInteger(verifier) * k)), N);

			return B.GetBytes();
		}

		public static byte[] ComputeU(byte[] clientPublicKey, byte[] serverPublicKey) // u
		{
			byte[] A = clientPublicKey;
			byte[] B = serverPublicKey;

			string one = NetUtility.ToHexString(A);
			string two = NetUtility.ToHexString(B);
			string compound = one.PadLeft(66, '0') + two.PadLeft(66, '0');

			byte[] cc = NetUtility.ToByteArray(compound);

			return NetSha.Hash(cc);
		}

		public static byte[] ComputeServerSessionKey(byte[] clientPublicKey, byte[] verifier, byte[] u, byte[] serverPrivateKey) // Ss
		{
			// S = (Av^u) ^ b (mod N)
			// return vv.modPow(uu, N).multiply(A).mod(N).modPow(bb, N);

			BigInteger verBi = new BigInteger(verifier);
			BigInteger uBi = new BigInteger(u);
			BigInteger ABi = new BigInteger(clientPublicKey); // A
			BigInteger bBi = new BigInteger(serverPrivateKey); // b

			Console.WriteLine("Ss input v: " + NetUtility.ToHexString(verifier));
			Console.WriteLine("Ss input u: " + NetUtility.ToHexString(u));
			Console.WriteLine("Ss input A: " + NetUtility.ToHexString(clientPublicKey));
			Console.WriteLine("Ss input A: " + ABi.ToString(16));
			Console.WriteLine("Ss input b: " + NetUtility.ToHexString(serverPrivateKey));

			BigInteger retval = verBi.ModPow(uBi, N).Multiply(ABi).Modulus(N).ModPow(bBi, N).Modulus(N);
			Console.WriteLine("Ss (trad): " + NetUtility.ToHexString(retval.GetBytes()));
			BigInteger f1 = verBi.ModPow(uBi, N);
			Console.WriteLine("f1 (trad): " + NetUtility.ToHexString(f1.GetBytes()));

			//return retval.GetBytes();


			// own
			// BigInteger tmp1 = verBi.ModPow(uBi, N).ModPow(bBi, N).Modulus(N);
			BigInteger tmp1 = (ABi * verBi.ModPow(uBi, N)).ModPow(bBi, N);
			Console.WriteLine("Ss (own): " + NetUtility.ToHexString(tmp1.GetBytes()));



			// bc
			BigIntegerBC verBi2 = new BigIntegerBC(verifier);
			BigIntegerBC ABi2 = new BigIntegerBC(clientPublicKey); // A
			BigIntegerBC uBi2 = new BigIntegerBC(u);
			BigIntegerBC bBi2 = new BigIntegerBC(serverPrivateKey);
			BigIntegerBC N2 = new BigIntegerBC(N.GetBytes());

			BigIntegerBC retval2 = verBi2.ModPow(uBi2, N2).Multiply(ABi2).Modulus(N2).ModPow(bBi2, N2).Modulus(N2);
			Console.WriteLine("Ss (bc): " + NetUtility.ToHexString(retval2.ToByteArray()));
			BigIntegerBC f12 = verBi2.ModPow(uBi2, N2);
			Console.WriteLine("f1 (bc): " + NetUtility.ToHexString(f12.ToByteArray()));
			

			// own bc
			BigIntegerBC tmp2 = verBi2.ModPow(uBi2, N2).ModPow(bBi2, N2).Modulus(N2);
			Console.WriteLine("Ss (ownBC): " + NetUtility.ToHexString(tmp2.ToByteArray()));



			return retval.GetBytes();

			//return NetSha.Hash(retval.GetBytes());
		}

		public static byte[] ComputeClientSessionKey(byte[] serverPublicKey, byte[] x, byte[] u, byte[] clientPrivateKey) // Sc
		{
			BigInteger xBi = new BigInteger(x);
			BigInteger BBi = new BigInteger(serverPublicKey); // B
			BigInteger uBi = new BigInteger(u);
			BigInteger aBi = new BigInteger(clientPrivateKey); // a

			BigInteger retval = (BBi + (N - ((k * g.ModPow(xBi, N)) % N))).ModPow(aBi + uBi * xBi, N);

			return retval.GetBytes();

			//return NetSha.Hash(retval.GetBytes());
		}		
	}
}
