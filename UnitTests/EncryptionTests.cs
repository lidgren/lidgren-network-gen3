using System;
using System.Text;

using Lidgren.Network;
using System.Security;
using System.Collections.Generic;

namespace UnitTests
{
	public static class EncryptionTests
	{
		public static void Run(NetPeer peer)
		{
			//
			// Test XTEA
			//
			List<INetEncryption> algos = new List<INetEncryption>();

			algos.Add(new NetXorEncryption("TopSecret"));
			algos.Add(new NetXtea("TopSecret"));

			foreach (var algo in algos)
			{
				NetOutgoingMessage om = peer.CreateMessage();
				om.Write("Hallon");
				om.Write(42);
				om.Write(5, 5);
				om.Write(true);
				om.Write("kokos");
				int trueLen = om.LengthBits;
				om.Encrypt(algo);

				// convert to incoming message
				NetIncomingMessage im = Program.CreateIncomingMessage(om.PeekDataBuffer(), om.LengthBits);
				im.Decrypt(algo);

				if (im.LengthBits != trueLen)
					throw new NetException("Length fail");

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

				Console.WriteLine(algo.GetType().Name + " encryption verified");
			}

			for (int i = 0; i < 100; i++)
			{
				byte[] salt = NetSRP.CreateRandomSalt();
				byte[] x = NetSRP.ComputePrivateKey("user", "password", salt);

				byte[] v = NetSRP.ComputeServerVerifier(x);
				//Console.WriteLine("v = " + NetUtility.ToHexString(v));

				byte[] a = NetSRP.CreateRandomEphemeral(); //  NetUtility.ToByteArray("393ed364924a71ba7258633cc4854d655ca4ec4e8ba833eceaad2511e80db2b5");
				byte[] A = NetSRP.ComputeClientEphemeral(a);
				//Console.WriteLine("A = " + NetUtility.ToHexString(A));

				byte[] b = NetSRP.CreateRandomEphemeral(); // NetUtility.ToByteArray("cc4d87a90db91067d52e2778b802ca6f7d362490c4be294b21b4a57c71cf55a9");
				byte[] B = NetSRP.ComputeServerEphemeral(b, v);
				//Console.WriteLine("B = " + NetUtility.ToHexString(B));

				byte[] u = NetSRP.ComputeU(A, B);
				//Console.WriteLine("u = " + NetUtility.ToHexString(u));

				byte[] Ss = NetSRP.ComputeServerSessionValue(A, v, u, b);
				//Console.WriteLine("Ss = " + NetUtility.ToHexString(Ss));

				byte[] Sc = NetSRP.ComputeClientSessionValue(B, x, u, a);
				//Console.WriteLine("Sc = " + NetUtility.ToHexString(Sc));

				if (Ss.Length != Sc.Length)
					throw new NetException("SRP non matching lengths!");

				for (int j = 0; j < Ss.Length; j++)
				{
					if (Ss[j] != Sc[j])
						throw new NetException("SRP non matching session values!");
				}

				var test = NetSRP.CreateEncryption(Ss);
			}

			Console.WriteLine("Message encryption OK");
		}
	}
}
