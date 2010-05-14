using System;
using System.Collections.Generic;

using Lidgren.Network;
using MSCommon;
using System.Net;

namespace MSServer
{
	class Program
	{
		static void Main(string[] args)
		{
			IPEndPoint masterServerEndpoint = NetUtility.Resolve("localhost", CommonConstants.MasterServerPort);

			NetPeerConfiguration config = new NetPeerConfiguration("game");
			config.Port = 14242;

			NetServer server = new NetServer(config);
			server.Start();

			Console.WriteLine("Server started; waiting 5 seconds...");
			System.Threading.Thread.Sleep(5000);

			// register with master server
			NetOutgoingMessage regMsg = server.CreateMessage();
			regMsg.Write((byte)MasterServerMessageType.RegisterHost);
			IPAddress mask;
			IPAddress adr = NetUtility.GetMyAddress(out mask);
			regMsg.Write(new IPEndPoint(adr, 14242));

			Console.WriteLine("Sending registration to master server");
			server.SendUnconnectedMessage(regMsg, masterServerEndpoint);

			Console.ReadKey();
		}
	}
}
