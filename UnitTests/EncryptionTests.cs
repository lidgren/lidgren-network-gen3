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
		}
	}
}
