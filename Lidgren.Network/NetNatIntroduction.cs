using System;
using System.Collections.Generic;
using System.Net;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		public void Introduce(
			IPEndPoint hostInternal,
			IPEndPoint hostExternal,
			IPEndPoint clientInternal,
			IPEndPoint clientExternal,
			string token)
		{
			// send message to client
			NetOutgoingMessage msg = CreateMessage(10 + token.Length + 1);
			msg.Write(false);
			msg.WritePadBits();
			msg.Write(hostInternal);
			msg.Write(hostExternal);
			msg.Write(token);
			SendUnconnectedLibraryMessage(msg, NetMessageLibraryType.NatIntroduction, clientExternal);

			// send message to host
			msg = CreateMessage(10 + token.Length + 1);
			msg.Write(true);
			msg.WritePadBits();
			msg.Write(clientInternal);
			msg.Write(clientExternal);
			msg.Write(token);
			SendUnconnectedLibraryMessage(msg, NetMessageLibraryType.NatIntroduction, hostExternal);
		}

		/// <summary>
		/// Called when host/client receives a NatIntroduction message from a master server
		/// </summary>
		private void HandleNatIntroduction(int ptr)
		{
			VerifyNetworkThread();

			// read intro
			NetIncomingMessage tmp = new NetIncomingMessage(m_receiveBuffer, 1000); // never mind length
			tmp.Position = (ptr * 8);
			byte hostByte = tmp.ReadByte();
			IPEndPoint remoteInternal = tmp.ReadIPEndpoint();
			IPEndPoint remoteExternal = tmp.ReadIPEndpoint();
			string token = tmp.ReadString();
			bool isHost = (hostByte != 0);

			NetOutgoingMessage punch;

			if (!isHost && m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.NatIntroductionSuccess) == false)
				return; // no need to punch - we're not listening for nat intros!

			// send internal punch
			punch = CreateMessage(1);
			punch.Write(hostByte);
			if (hostByte == 0)
			{
				// only client needs to send token
				punch.Write(token);
			}
			SendUnconnectedLibraryMessage(punch, NetMessageLibraryType.NatPunchMessage, remoteInternal);

			// send external punch
			punch = CreateMessage(1);
			punch.Write(hostByte);
			if (hostByte == 0)
			{
				// only client needs to send token
				punch.Write(token);
			}
			SendUnconnectedLibraryMessage(punch, NetMessageLibraryType.NatPunchMessage, remoteExternal);
		}

		/// <summary>
		/// Called when receiving a NatPunchMessage from a remote endpoint
		/// </summary>
		private void HandleNatPunch(int ptr, IPEndPoint senderEndpoint)
		{
			NetIncomingMessage tmp = new NetIncomingMessage(m_receiveBuffer, 1000); // never mind length
			tmp.Position = (ptr * 8);

			byte hostByte = tmp.ReadByte();
			if (hostByte != 0)
				return; // don't alert hosts about nat punch successes; only clients
			string token = tmp.ReadString();

			//
			// Release punch success to client; enabling him to Connect() to msg.SenderIPEndPoint if token is ok
			//
			NetIncomingMessage punchSuccess = CreateIncomingMessage(NetIncomingMessageType.NatIntroductionSuccess, 10);
			punchSuccess.m_senderEndpoint = senderEndpoint;
			punchSuccess.Write(token);
			ReleaseMessage(punchSuccess);
		}
	}
}
