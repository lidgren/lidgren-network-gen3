using System;

using Lidgren.Network;

namespace UnitTests
{
	public static class MiscTests
	{
		public static void Run(NetPeer peer)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("Test");

			config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			if (config.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData) == false)
				throw new NetException("setting enabled message types failed");

			config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, false);
			if (config.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData) == true)
				throw new NetException("setting enabled message types failed");

			Console.WriteLine("Misc tests OK");
		}
	}
}
