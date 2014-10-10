using Lidgren.Network;
using System;

namespace EncryptionServer
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new NetPeerConfiguration("enctest");
			config.MaximumConnections = 1;
			config.Port = 14242;
			var server = new NetServer(config);
			server.Start();

			var encryption = new NetAESEncryption(server, "Hallonpalt");

			// loop forever
			while (true)
			{
				var inc = server.ReadMessage();
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
						case NetIncomingMessageType.Data:
							var ok = inc.Decrypt(encryption);
							Console.WriteLine("Data (decrypted: " + (ok ? "ok" : "fail") + ") " + inc.ReadString());
							break;
					}
				}
				System.Threading.Thread.Sleep(1);
			}
		}
	}
}
