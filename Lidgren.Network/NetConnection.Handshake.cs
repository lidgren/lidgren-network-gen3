/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;

namespace Lidgren.Network
{
	public partial class NetConnection
	{
		internal bool m_connectRequested;
		internal string m_disconnectByeMessage;
		internal bool m_connectionInitiator;
		internal double m_connectInitationTime; // regardless of initiator
		internal NetOutgoingMessage m_approvalMessage;

		internal void SetStatus(NetConnectionStatus status, string reason)
		{
			m_owner.VerifyNetworkThread();

			if (status == m_status)
				return;
			m_status = status;
			if (reason == null)
				reason = string.Empty;

			if (m_peerConfiguration.IsMessageTypeEnabled(NetIncomingMessageType.StatusChanged))
			{
				NetIncomingMessage info = m_owner.CreateIncomingMessage(NetIncomingMessageType.StatusChanged, 4 + reason.Length + (reason.Length > 126 ? 2 : 1));
				info.m_senderConnection = this;
				info.m_senderEndpoint = m_remoteEndpoint;
				info.Write((byte)m_status);
				info.Write(reason);
				m_owner.ReleaseMessage(info);
			}
		}

		private void SendConnect()
		{
			m_owner.VerifyNetworkThread();

			switch (m_status)
			{
				case NetConnectionStatus.Connected:
					// reconnect
					m_disconnectByeMessage = "Reconnecting";
					ExecuteDisconnect(true);
					FinishDisconnect();
					break;
				case NetConnectionStatus.Connecting:
				case NetConnectionStatus.None:
					break;
				case NetConnectionStatus.Disconnected:
					throw new NetException("This connection is Disconnected; spent. A new one should have been created");

				case NetConnectionStatus.Disconnecting:
					// let disconnect finish first
					return;
			}

			m_connectRequested = false;

			// start handshake

			int len = 2 + m_peerConfiguration.AppIdentifier.Length + 8 + 4 + (m_approvalMessage == null ? 0 : m_approvalMessage.LengthBytes);
			NetOutgoingMessage om = m_owner.CreateMessage(len);
			om.m_type = NetMessageType.Library;
			om.m_libType = NetMessageLibraryType.Connect;
			om.Write(m_peerConfiguration.AppIdentifier);
			om.Write(m_owner.m_uniqueIdentifier);

			if (m_approvalMessage == null)
			{
				om.WriteVariableUInt32(0);
			}
			else
			{
				om.WriteVariableUInt32((uint)m_approvalMessage.LengthBits);
				om.Write(m_approvalMessage);
			}
			m_owner.LogVerbose("Sending Connect");

			m_owner.SendImmediately(this, om);

			m_connectInitationTime = NetTime.Now;
			SetStatus(NetConnectionStatus.Connecting, "Connecting");

			return;
		}

		internal void SendConnectResponse()
		{
			NetOutgoingMessage reply = m_owner.CreateMessage(4);
			reply.m_type = NetMessageType.Library;
			reply.m_libType = NetMessageLibraryType.ConnectResponse;
			reply.Write((float)NetTime.Now);

			m_owner.LogVerbose("Sending LibraryConnectResponse");
			m_owner.SendImmediately(this, reply);
		}

		internal void SendConnectionEstablished()
		{
			NetOutgoingMessage ce = m_owner.CreateMessage(4);
			ce.m_type = NetMessageType.Library;
			ce.m_libType = NetMessageLibraryType.ConnectionEstablished;
			ce.Write((float)NetTime.Now);

			m_owner.LogVerbose("Sending LibraryConnectionEstablished");
			m_owner.SendImmediately(this, ce);
		}

		internal void ExecuteDisconnect(bool sendByeMessage)
		{
			m_owner.VerifyNetworkThread();

			if (m_status == NetConnectionStatus.Disconnected || m_status == NetConnectionStatus.None)
				return;

			if (sendByeMessage)
			{
				NetOutgoingMessage om = m_owner.CreateLibraryMessage(NetMessageLibraryType.Disconnect, m_disconnectByeMessage);
				EnqueueOutgoingMessage(om);
			}

			m_owner.LogVerbose("Executing Disconnect(" + m_disconnectByeMessage + ")");

			return;
		}

		private void FinishDisconnect()
		{
			m_owner.VerifyNetworkThread();

			if (m_status == NetConnectionStatus.Disconnected || m_status == NetConnectionStatus.None)
				return;

			m_owner.LogVerbose("Finishing Disconnect(" + m_disconnectByeMessage + ")");

			// release some held memory
			if (m_storedMessages != null)
			{
				foreach (List<NetOutgoingMessage> oml in m_storedMessages)
					if (oml != null)
						oml.Clear();
			}
			m_acknowledgesToSend.Clear();

			SetStatus(NetConnectionStatus.Disconnected, m_disconnectByeMessage);
			m_disconnectByeMessage = null;
			m_connectionInitiator = false;
		}

		private void HandleIncomingHandshake(NetMessageLibraryType ltp, int ptr, int payloadBitsLength)
		{
			m_owner.VerifyNetworkThread();

			switch (ltp)
			{
				case NetMessageLibraryType.Connect:
					if (m_status == NetConnectionStatus.Connecting)
					{
						// our connectresponse must have been lost, send another one
						SendConnectResponse();
						return;
					}

					m_owner.LogError("NetConnection.HandleIncomingHandshake() passed LibraryConnect but status is " + m_status + "!?");
					break;
				case NetMessageLibraryType.ConnectResponse:
					if (!m_connectionInitiator)
					{
						m_owner.LogError("NetConnection.HandleIncomingHandshake() passed LibraryConnectResponse, but we're not initiator!");
						// weird, just drop it
						return;
					}

					if (m_status == NetConnectionStatus.Connecting)
					{
						m_owner.m_statistics.m_bytesAllocated += NetUtility.BytesToHoldBits(payloadBitsLength);

						float remoteNetTime = BitConverter.ToSingle(m_owner.m_receiveBuffer, ptr);
						ptr += 4;

						// excellent, handshake making progress; send connectionestablished
						SetStatus(NetConnectionStatus.Connected, "Connected");

						SendConnectionEstablished();

						// setup initial ping estimation
						InitializeLatency((float)(NetTime.Now - m_connectInitationTime), remoteNetTime);
						return;
					}

					if (m_status == NetConnectionStatus.Connected)
					{
						// received (another) connectresponse; our connectionestablished must have been lost, send another one
						SendConnectionEstablished();
						return;
					}

					m_owner.LogWarning("NetConnection.HandleIncomingHandshake() passed " + ltp + ", but status is " + m_status);
					break;
				case NetMessageLibraryType.ConnectionEstablished:
					if (!m_connectionInitiator && m_status == NetConnectionStatus.Connecting)
					{
						float remoteNetTime = BitConverter.ToSingle(m_owner.m_receiveBuffer, ptr);

						// handshake done
						InitializeLatency((float)(NetTime.Now - m_connectInitationTime), remoteNetTime);

						SetStatus(NetConnectionStatus.Connected, "Connected");
						return;
					}

					m_owner.LogWarning("NetConnection.HandleIncomingHandshake() passed " + ltp + ", but initiator is " + m_connectionInitiator + " and status is " + m_status);
					break;
				case NetMessageLibraryType.Disconnect:
					// extract bye message
					NetIncomingMessage im = m_owner.CreateIncomingMessage(NetIncomingMessageType.Data, m_owner.m_receiveBuffer, ptr, NetUtility.BytesToHoldBits(payloadBitsLength));
					im.m_bitLength = payloadBitsLength;
					m_disconnectByeMessage = im.ReadString();
					FinishDisconnect();
					break;
				default:
					// huh?
					throw new NotImplementedException("Unhandled library type: " + ltp);
			}
		}
	}
}
