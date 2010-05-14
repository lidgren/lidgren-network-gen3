using System;
using System.Text;

using Lidgren.Network;

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

			byte[] test = new byte[16];
			NetRandom.Instance.NextBytes(test);

			byte[] encrypted = new byte[test.Length];
			xtea.EncryptBlock(test, 0, encrypted, 0);

			byte[] decrypted = new byte[test.Length];
			xtea.DecryptBlock(encrypted, 0, decrypted, 0);

			// compare!
			for (int i = 0; i < test.Length; i++)
				if (test[i] != decrypted[i])
					throw new NetException("XTEA fail!");


			NetOutgoingMessage om = peer.CreateMessage();
			om.Write("Hallon");
			om.Write(42);
			om.Write(5, 5);
			om.Write(true);
			om.Write("kokos");

			Console.WriteLine("Pre encryption: " + NetUtility.ToHexString(om.PeekDataBuffer()));
			om.Encrypt(xtea);
			Console.WriteLine("Post encryption: " + NetUtility.ToHexString(om.PeekDataBuffer()));

			// convert to incoming message
			NetIncomingMessage im = Program.CreateIncomingMessage(om.PeekDataBuffer(), om.LengthBits);
			Console.WriteLine("Pre decryption: " + NetUtility.ToHexString(im.PeekDataBuffer()));
			im.Decrypt(xtea);
			Console.WriteLine("Post decryption: " + NetUtility.ToHexString(im.PeekDataBuffer()));

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
