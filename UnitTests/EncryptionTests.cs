using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace UnitTests
{
	public static class EncryptionTests
	{
		public static void Run()
		{
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
