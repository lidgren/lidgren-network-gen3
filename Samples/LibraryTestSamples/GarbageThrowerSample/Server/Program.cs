using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace Server
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("garbagethrower");
			config.MaximumConnections = 1;
			config.Port = 14242;
			config.PingInterval = 2.0f;
			config.ConnectionTimeout = 2.0f;
			var server = new NetServer(config);

			server.Start();

			while (true)
			{
				NetIncomingMessage msg;
				while ((msg = server.ReadMessage()) != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.StatusChanged:
							var status = (NetConnectionStatus)msg.ReadByte();
							var reason = msg.ReadString();
							Console.WriteLine("New status: " + status + " (" + reason + ")");
							break;
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.DebugMessage:

							var str = msg.ReadString();
							if (str.StartsWith("Malformed packet; stated") ||
								str.StartsWith("Received unhandled library message") ||
								str.StartsWith("Unexpected NetMessageType"))
								break; // we'll get a bunch of these and we're fine with that

							Console.WriteLine(msg.MessageType + ": " + str);
							break;
						case NetIncomingMessageType.Data:
							Console.WriteLine("Received " + msg.LengthBits + " bits of data");
							break;
						case NetIncomingMessageType.UnconnectedData:
							Console.WriteLine("Received " + msg.LengthBits + " bits of unconnected data");
							break;
						default:
							Console.WriteLine("Received " + msg.MessageType);
							break;
					}
				}
			}
		}
	}
}
