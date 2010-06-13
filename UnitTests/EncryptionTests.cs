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

			/*
			Console.WriteLine("x = " + NetUtility.ToHexString(x));
			Console.WriteLine("v = " + NetUtility.ToHexString(verifier));
			Console.WriteLine("");

			byte[] a = NetUtility.ToByteArray("d378cc3b09d12cfca5130e22df3f2f3bcf8ecfaddeae6af7f8b3e9f8b4fc9749"); // random
			Console.WriteLine("a = " + NetUtility.ToHexString(a));
			byte[] A = NetSRP.ComputeClientChallenge(a);
			Console.WriteLine("A = " + NetUtility.ToHexString(A));
			Console.WriteLine("");

			byte[] b = NetUtility.ToByteArray("8394bdaebe1709124f4c1221707053440b30e270d457ece02818da63b53c2482"); // random
			Console.WriteLine("b = " + NetUtility.ToHexString(b)); 
			byte[] B = NetSRP.ComputeServerChallenge(b, verifier);
			Console.WriteLine("B = " + NetUtility.ToHexString(B));
			Console.WriteLine("");

			byte[] u = NetSRP.ComputeU(A, B);
			Console.WriteLine("u = " + NetUtility.ToHexString(u));

			byte[] serverCompareValue; // Ss
			serverCompareValue = NetSRP.ComputeServerCompareValue(A, verifier, u, b);
			Console.WriteLine("Ss = " + NetUtility.ToHexString(serverCompareValue));

			byte[] clientCompareValue; // Ss
			clientCompareValue = NetSRP.ComputeClientCompareValue(B, x, u, a);
			Console.WriteLine("Sc = " + NetUtility.ToHexString(clientCompareValue));
			*/

			//
			// Pre-step:
			//
			// Server must store { Username, salt, verifier }
			//byte[] salt = NetSRP.CreateRandomSalt();
			byte[] salt = NetUtility.ToByteArray("D016E5A43F0E2A1C8FF8");
			Console.WriteLine("s = " + NetUtility.ToHexString(salt));
			byte[] serverVerifier;
			byte[] clientVerifier;
			NetSRP.ComputePasswordVerifier("user", "password", salt, out serverVerifier, out clientVerifier);
			//
			Console.WriteLine("x = " + NetUtility.ToHexString(clientVerifier));
			Console.WriteLine("v = " + NetUtility.ToHexString(serverVerifier));

			// CLIENT:
			// Step 1: Client creates session private/public key pair
			//byte[] clientPrivateKey = NetSRP.CreateRandomKey();
			byte[] clientPrivateKey = NetUtility.ToByteArray("EFDBE24D15173DC1FBA22A8D51077AE932841CB2DBA8B09B2CFC5543983B2C7A");
			Console.WriteLine("a = " + NetUtility.ToHexString(clientPrivateKey));
			byte[] clientPublicKey = NetSRP.ComputeClientPublicKey(clientPrivateKey);
			// Step 2: Client sends username and client public key to server

			// SERVER:
			// Step 3: Server creates session private/public key pair
			//byte[] serverPrivateKey = NetSRP.CreateRandomKey();
			byte[] serverPrivateKey = NetUtility.ToByteArray("FB1D472CD89EAF323DB0F7DE80A01CC51DD5A0D1AFC8B79F3CF5A2FC88529ADC");
			Console.WriteLine("b = " + NetUtility.ToHexString(serverPrivateKey));
			byte[] serverPublicKey = NetSRP.ComputeServerPublicKey(serverPrivateKey, serverVerifier);
			// Step 4: Server sends salt and server public key to client

			// CLIENT:
			// Step 5: Client computes u and compare value
			byte[] u = NetSRP.ComputeU(clientPublicKey, serverPublicKey);
			Console.WriteLine("u = " + NetUtility.ToHexString(u));
			byte[] clientSessionKey = NetSRP.ComputeClientSessionKey(serverPublicKey, clientVerifier, u, clientPrivateKey); // this is where client proves it has x (and thus the password)
			Console.WriteLine("Sc = " + NetUtility.ToHexString(clientSessionKey));

			// SERVER:
			// Step 6: Server computes u and compare value
			// byte[] u = NetSRP.ComputeU(clientPublicKey, serverPublicKey);
			byte[] serverSessionKey = NetSRP.ComputeServerSessionKey(clientPublicKey, serverVerifier, u, serverPrivateKey);
			Console.WriteLine("Ss = " + NetUtility.ToHexString(serverSessionKey));

			if (!NetUtility.CompareElements(clientSessionKey, serverSessionKey))
				Console.WriteLine("BAD!!!!!!!");

			// Console.WriteLine("SRP test OK");
/*
C ==> S  C, A  
C <== S  s, B  
C ==> S  M[1]  
C <== S  M[2] 
			*/

		}
	}
}
