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
			byte[] salt = NetUtility.ToByteArray("59d7304da9b97e2a9d38");
			byte[] verifier = NetSRP.ComputePasswordVerifier("user", "password", salt);

			Console.WriteLine("Result: " + NetUtility.ToHexString(verifier));
		}
	}
}
