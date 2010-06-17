using System;
using System.Text;
using System.Threading;

using Lidgren.Network;

namespace BarebonesClient
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("barebones");
			config.SimulatedLoss = 0.1f;
			NetClient client = new NetClient(config);
			client.Start();

			Thread.Sleep(2000);

			client.Connect("localhost", 14242);

			while (Console.KeyAvailable == false || Console.ReadKey().Key != ConsoleKey.Escape)
			{
				NetIncomingMessage inc;
				while ((inc = client.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
							string reason = inc.ReadString();
							Console.WriteLine("New status: " + status + " (" + reason + ")");
							if (status == NetConnectionStatus.Connected)
							{
								//
								// We're connected - send stuff
								//
								NetOutgoingMessage om = client.CreateMessage();

								// temporary code to verify issue with large messages
								StringBuilder bdr = new StringBuilder();
								for (int i = 0; i < 1000; i++)
									bdr.Append("Hallonsmurf" + i.ToString());
								om.Write(bdr.ToString());

								client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
							}
							break;
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							Console.WriteLine(inc.ReadString());
							break;
						case NetIncomingMessageType.UnconnectedData:
							Console.WriteLine("Received unconnected data from " + inc.SenderEndpoint + ", conn is " + inc.SenderConnection);
							break;
						case NetIncomingMessageType.Data:
							Console.WriteLine("Received " + inc.LengthBytes + " bytes of data from " + inc.SenderConnection + ", endpoint is " + inc.SenderEndpoint);
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
