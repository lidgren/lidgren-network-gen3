using System;
using System.Text;

using Lidgren.Network;
using System.Security;

namespace UnitTests
{
	public static class EncryptionTests
	{
		public static void Run(NetPeer peer)
		{
			//
			// Test XTEA
			//
			NetXtea xtea = new NetXtea(NetSha.Hash(Encoding.ASCII.GetBytes("TopSecret")));

			byte[] original = new byte[16];
			NetRandom.Instance.NextBytes(original);

			byte[] encrypted = new byte[original.Length];
			xtea.EncryptBlock(original, 0, encrypted, 0);
			xtea.EncryptBlock(original, 8, encrypted, 8);

			byte[] decrypted = new byte[original.Length];
			xtea.DecryptBlock(encrypted, 0, decrypted, 0);
			xtea.DecryptBlock(encrypted, 8, decrypted, 8);

			// compare!
			for (int i = 0; i < original.Length; i++)
				if (original[i] != decrypted[i])
					throw new NetException("XTEA fail!");

			Console.WriteLine("XTEA OK");

			NetOutgoingMessage om = peer.CreateMessage();
			om.Write("Hallon");
			om.Write(42);
			om.Write(5, 5);
			om.Write(true);
			om.Write("kokos");
			om.Encrypt(xtea);

			// convert to incoming message
			NetIncomingMessage im = Program.CreateIncomingMessage(om.PeekDataBuffer(), om.LengthBits);
			im.Decrypt(xtea);

			if (im.ReadString() != "Hallon")
				throw new NetException("fail");
			if (im.ReadInt32() != 42)
				throw new NetException("fail");
			if (im.ReadInt32(5) != 5)
				throw new NetException("fail");
			if (im.ReadBoolean() != true)
				throw new NetException("fail");
			if (im.ReadString() != "kokos")
				throw new NetException("fail");

			Console.WriteLine("Message encryption OK");

			byte[] salt = NetUtility.ToByteArray("e6fb7e23f001f3e6c081"); // s
			byte[] verifier = NetSRP.ComputePasswordVerifier("user", "password", salt);

			Console.WriteLine("v = " + NetUtility.ToHexString(verifier));

			byte[] a = NetUtility.ToByteArray("3b6485358d1721cb438cb7d0b3c5f8f46186d43e1c47db7cd8aa80e19760e409");
			byte[] A = NetSRP.ComputeClientChallenge(a);
			Console.WriteLine("A = " + NetUtility.ToHexString(A));

			byte[] b = NetUtility.ToByteArray("fc17d424ce73a4c73e8fedfb25839e9917e861bc5253fff65697f81c75a87ea3");
			Console.WriteLine("b = " + NetUtility.ToHexString(b)); 
			byte[] B = NetSRP.ComputeServerChallenge(b, verifier);
			Console.WriteLine("B = " + NetUtility.ToHexString(B));

			byte[] u = NetSRP.ComputeU(A, B);
			Console.WriteLine("u = " + NetUtility.ToHexString(u));
		}
	}
}
