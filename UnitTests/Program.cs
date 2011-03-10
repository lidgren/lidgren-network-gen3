using System;
using System.Reflection;
using Lidgren.Network;

namespace UnitTests
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("unittests");
			NetPeer peer = new NetPeer(config);
			peer.Start(); // needed for initialization

			System.Threading.Thread.Sleep(50);

			Console.WriteLine("Unique identifier is " + NetUtility.ToHexString(peer.UniqueIdentifier));

			ReadWriteTests.Run(peer);

			NetQueueTests.Run();

			MiscTests.Run(peer);

			BitVectorTests.Run();

			EncryptionTests.Run(peer);

			peer.Shutdown("bye");

			// read all message
			NetIncomingMessage inc;
			while((inc = peer.ReadMessage()) != null)
			{
				switch(inc.MessageType)
				{
					case NetIncomingMessageType.DebugMessage:
					case NetIncomingMessageType.VerboseDebugMessage:
					case NetIncomingMessageType.WarningMessage:
					case NetIncomingMessageType.ErrorMessage:
						Console.WriteLine("Peer message: " + inc.ReadString());
						break;
					case NetIncomingMessageType.Error:
						throw new Exception("Received error message!");
				}
			}
			
			Console.WriteLine("Done");
			Console.ReadKey();
		}

		public static NetIncomingMessage CreateIncomingMessage(byte[] fromData, int bitLength)
		{
			NetIncomingMessage inc = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
			typeof(NetIncomingMessage).GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, fromData);
			typeof(NetIncomingMessage).GetField("m_bitLength", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, bitLength);
			return inc;
		}
	}
}
