using System;
using System.Collections.Generic;
using System.Net;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		public void Introduce(IPEndPoint host, IPEndPoint client)
		{
			// send message to client
			NetOutgoingMessage msg = CreateMessage(10);
			msg.Write(false);
			msg.WritePadBits();
			msg.Write(host);
			SendUnconnectedLibraryMessage(msg, NetMessageLibraryType.NatIntroduction, client);

			// send message to host
			msg = CreateMessage(10);
			msg.Write(true);
			msg.WritePadBits();
			msg.Write(client);
			SendUnconnectedLibraryMessage(msg, NetMessageLibraryType.NatIntroduction, host);
		}

		private void HandleNatIntroduction(int ptr, IPEndPoint senderEndpoint)
		{
			VerifyNetworkThread();

			// read intro
			NetIncomingMessage tmp = new NetIncomingMessage(m_receiveBuffer, 1000); // never mind length
			tmp.Position = (ptr * 8);
			bool isHost = (tmp.ReadByte() == 0 ? false : true);
			IPEndPoint ep = tmp.ReadIPEndpoint();

			// quickly; send nat punch
			NetOutgoingMessage punch = CreateMessage(0);
			SendUnconnectedLibraryMessage(punch, NetMessageLibraryType.NatPunchMessage, ep);

			if (!isHost)
			{
				NetIncomingMessage intro = CreateIncomingMessage(NetIncomingMessageType.NatIntroduction, 10);
				intro.Write(ep);
				intro.m_senderEndpoint = senderEndpoint;
				ReleaseMessage(intro);
			}
		}
	}
}
