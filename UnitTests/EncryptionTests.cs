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
			NetXtea xtea = new NetXtea("TopSecret");

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

			byte[] salt = NetUtility.ToByteArray("62191568b7a1aa18f8eb"); // s
			byte[] verifier = NetSRP.ComputePasswordVerifier("user", "password", salt);

			Console.WriteLine("v = " + NetUtility.ToHexString(verifier));

			byte[] a = NetUtility.ToByteArray("129aac7ce0be45ab5f65ec0c6879222386c32177cb4024fe7ad593341c0a5085");
			byte[] A = NetSRP.ComputeClientChallenge(a);
			Console.WriteLine("A = " + NetUtility.ToHexString(A));

			byte[] b = NetUtility.ToByteArray("cdbe8cec49e33c78c0b434be67fa2fdb7646776e757bcf59fad51bbbee0d53a1");
			Console.WriteLine("b = " + NetUtility.ToHexString(b)); 
			byte[] B = NetSRP.ComputeServerChallenge(b, verifier);
			Console.WriteLine("B = " + NetUtility.ToHexString(B));

			byte[] u = NetSRP.ComputeU(A, B);
			Console.WriteLine("u = " + NetUtility.ToHexString(u));
		}
	}
}
