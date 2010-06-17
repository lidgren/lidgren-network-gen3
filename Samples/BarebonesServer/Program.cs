using System;
using System.Text;
using System.Threading;

using Lidgren.Network;

namespace BarebonesServer
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("barebones");
			config.Port = 14242;
			config.SimulatedLoss = 0.1f;
			NetServer server = new NetServer(config);
			server.Start();

			NetIncomingMessage inc;
			while (Console.KeyAvailable == false || Console.ReadKey().Key != ConsoleKey.Escape)
			{
				while ((inc = server.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							Console.WriteLine(inc.ReadString());
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
							string reason = inc.ReadString();
							Console.WriteLine("New status: " + status + " (" + reason + ")");
							break;
						case NetIncomingMessageType.UnconnectedData:
							Console.WriteLine("Received unconnected data from " + inc.SenderEndpoint + ", conn is " + inc.SenderConnection);
							break;
						case NetIncomingMessageType.Data:
							Console.WriteLine("Received " + inc.LengthBytes + " bytes of data from " + inc.SenderConnection + ", endpoint is " + inc.SenderEndpoint);

							// temporary code to verify issue with large messages
							StringBuilder bdr = new StringBuilder();
							for (int i = 0; i < 1000; i++)
								bdr.Append("Hallonsmurf" + i.ToString());

							string str = inc.ReadString();

							Console.WriteLine("Compare gives: " + str.CompareTo(bdr.ToString()));

							break;
					}
				}

				Thread.Sleep(1);
			}

			Console.WriteLine("Application exiting");
			while (true) ;
		}
	}
}
