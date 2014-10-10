using System;

using Lidgren.Network;

namespace EncryptionClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new NetPeerConfiguration("enctest");
			var client = new NetClient(config);
			client.Start();

			System.Threading.Thread.Sleep(100); // give server time to start up

			client.Connect("localhost", 14242);

			var encryption = new NetAESEncryption(client, "Hallonpalt");

			// loop forever
			while (true)
			{
				// read messages
				var inc = client.ReadMessage();
				if (inc != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.ErrorMessage:
							Console.WriteLine(inc.ReadString());
							break;
						case NetIncomingMessageType.StatusChanged:
							var status = (NetConnectionStatus)inc.ReadByte();
							Console.WriteLine(inc.SenderConnection + " (" + status + ") " + inc.ReadString());
							break;
					}
				}

				// if we're connected, get input and send
				if (client.ServerConnection != null && client.ServerConnection.Status == NetConnectionStatus.Connected)
				{
					Console.WriteLine("Type a message:");
					var input = Console.ReadLine();

					var msg = client.CreateMessage();
					msg.Write(input);
					encryption.Encrypt(msg);

					var ok = client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
					Console.WriteLine("Message sent: " + ok);
				}
			}
		}
	}
}
