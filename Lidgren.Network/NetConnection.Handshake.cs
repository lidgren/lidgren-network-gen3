using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public partial class NetConnection
	{
		internal bool m_connectRequested;
		internal bool m_disconnectRequested;
		internal bool m_connectionInitiator;
		internal string m_disconnectMessage;
		internal NetIncomingMessage m_remoteHailMessage;

		/// <summary>
		/// The message that the remote part specified via Connect() or Approve() - can be null.
		/// </summary>
		public NetIncomingMessage RemoteHailMessage { get { return m_remoteHailMessage; } }

		internal void UnconnectedHeartbeat(float now)
		{
			m_peer.VerifyNetworkThread();

			if (m_disconnectRequested)
				ExecuteDisconnect(m_disconnectMessage, true);

			if (m_connectRequested)
			{
				switch (m_status)
				{
					case NetConnectionStatus.Connected:
					case NetConnectionStatus.RespondedConnect:
						// reconnect
						ExecuteDisconnect("Reconnecting", true);
						break;
					case NetConnectionStatus.InitiatedConnect:
						// send another connect attempt
						SendConnect();
						break;
					case NetConnectionStatus.Disconnected:
						throw new NetException("This connection is Disconnected; spent. A new one should have been created");

					case NetConnectionStatus.Disconnecting:
						// let disconnect finish first
						return;
					case NetConnectionStatus.None:
					default:
						SendConnect();
						break;
				}
				return;
			}

			// TODO: handle dangling connections
		}

		internal void ExecuteDisconnect(string reason, bool sendByeMessage)
		{
			m_peer.VerifyNetworkThread();

			// m_peer.LogDebug("Executing disconnect");

			// clear send queues
			for (int i = 0; i < m_sendChannels.Length; i++)
			{
				NetSenderChannelBase channel = m_sendChannels[i];
				if (channel != null)
					channel.Reset();
			}

			if (sendByeMessage)
				SendDisconnect(reason, true);

			SetStatus(NetConnectionStatus.Disconnected, reason);
			m_disconnectRequested = false;
			m_connectRequested = false;
		}

		internal void SendConnect()
		{
			NetOutgoingMessage om = m_peer.CreateMessage(m_peerConfiguration.AppIdentifier.Length + 1 + 4);
			om.m_messageType = NetMessageType.Connect;
			om.Write(m_peerConfiguration.AppIdentifier);
			om.Write(m_peer.m_uniqueIdentifier);
			om.Write((float)NetTime.Now);

			WriteLocalHail(om);
			
			m_peer.SendLibrary(om, m_remoteEndpoint);

			SetStatus(NetConnectionStatus.InitiatedConnect, "Locally requested connect");
			m_connectRequested = false;
		}

		internal void SendConnectResponse(bool onLibraryThread)
		{
			NetOutgoingMessage om = m_peer.CreateMessage(m_peerConfiguration.AppIdentifier.Length + 1 + 4);
			om.m_messageType = NetMessageType.ConnectResponse;
			om.Write(m_peerConfiguration.AppIdentifier);
			om.Write(m_peer.m_uniqueIdentifier);
			om.Write((float)NetTime.Now);

			WriteLocalHail(om);

			if (onLibraryThread)
				m_peer.SendLibrary(om, m_remoteEndpoint);
			else
				m_peer.m_unsentUnconnectedMessages.Enqueue(new NetTuple<System.Net.IPEndPoint, NetOutgoingMessage>(m_remoteEndpoint, om));

			SetStatus(NetConnectionStatus.RespondedConnect, "Remotely requested connect");
		}

		internal void SendDisconnect(string reason, bool onLibraryThread)
		{
			NetOutgoingMessage om = m_peer.CreateMessage(reason);
			om.m_messageType = NetMessageType.Disconnect;
			if (onLibraryThread)
				m_peer.SendLibrary(om, m_remoteEndpoint);
			else
				m_peer.m_unsentUnconnectedMessages.Enqueue(new NetTuple<System.Net.IPEndPoint, NetOutgoingMessage>(m_remoteEndpoint, om));
		}

		private void WriteLocalHail(NetOutgoingMessage om)
		{
			if (m_localHailMessage != null)
			{
				byte[] hi = m_localHailMessage.PeekDataBuffer();
				if (hi != null && hi.Length >= m_localHailMessage.LengthBytes)
				{
					if (om.LengthBytes + m_localHailMessage.LengthBytes > m_peerConfiguration.m_maximumTransmissionUnit - 10)
						throw new NetException("Hail message too large; can maximally be " + (m_peerConfiguration.m_maximumTransmissionUnit - 10 - om.LengthBytes));
					om.Write(m_localHailMessage.PeekDataBuffer(), 0, m_localHailMessage.LengthBytes);
				}
			}
		}

		internal void SendConnectionEstablished()
		{
			NetOutgoingMessage om = m_peer.CreateMessage(0);
			om.m_messageType = NetMessageType.ConnectionEstablished;
			om.Write((float)NetTime.Now);
			m_peer.SendLibrary(om, m_remoteEndpoint);

			InitializePing();
			if (m_status != NetConnectionStatus.Connected)
				SetStatus(NetConnectionStatus.Connected, "Connected to " + NetUtility.ToHexString(m_remoteUniqueIdentifier));
		}

		/// <summary>
		/// Approves this connection; sending a connection response to the remote host
		/// </summary>
		public void Approve()
		{
			m_localHailMessage = null;
			SendConnectResponse(false);
		}

		/// <summary>
		/// Approves this connection; sending a connection response to the remote host
		/// </summary>
		/// <param name="localHail">The local hail message that will be set as RemoteHailMessage on the remote host</param>
		public void Approve(NetOutgoingMessage localHail)
		{
			m_localHailMessage = localHail;
			SendConnectResponse(false);
		}

		/// <summary>
		/// Denies this connection; disconnecting it
		/// </summary>
		public void Deny()
		{
			Deny("");
		}

		/// <summary>
		/// Denies this connection; disconnecting it
		/// </summary>
		/// <param name="reason">The stated reason for the disconnect, readable as a string in the StatusChanged message on the remote host</param>
		public void Deny(string reason)
		{
			// send disconnect; remove from handshakes
			SendDisconnect(reason, false);

			// remove from handshakes
			m_peer.m_handshakes.Remove(m_remoteEndpoint); // TODO: make this more thread safe? we're on user thread
		}

		internal void ReceivedHandshake(double now, NetMessageType tp, int ptr, int payloadLength)
		{
			m_peer.VerifyNetworkThread();

			byte[] hail;
			switch (tp)
			{
				case NetMessageType.Connect:
					if (m_status == NetConnectionStatus.None)
					{
						// Whee! Server full has already been checked
						bool ok = ValidateHandshakeData(ptr, payloadLength, out hail);
						if (ok)
						{
							if (hail != null)
							{
								m_remoteHailMessage = m_peer.CreateIncomingMessage(NetIncomingMessageType.Data, hail);
								m_remoteHailMessage.LengthBits = (hail.Length * 8);
							}
							else
							{
								m_remoteHailMessage = null; 
							}

							if (m_peerConfiguration.IsMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval))
							{
								// ok, let's not add connection just yet
								NetIncomingMessage appMsg = m_peer.CreateIncomingMessage(NetIncomingMessageType.ConnectionApproval, (m_remoteHailMessage == null ? 0 : m_remoteHailMessage.LengthBytes));
								appMsg.m_receiveTime = now;
								appMsg.m_senderConnection = this;
								appMsg.m_senderEndpoint = this.m_remoteEndpoint;
								if (m_remoteHailMessage != null)
									appMsg.Write(m_remoteHailMessage.m_data, 0, m_remoteHailMessage.LengthBytes);
								m_peer.ReleaseMessage(appMsg);
								return;
							}

							SendConnectResponse(true);
						}
						return;
					}
					if (m_status == NetConnectionStatus.RespondedConnect)
					{
						// our ConnectResponse must have been lost
						SendConnectResponse(true);
						return;
					}
					m_peer.LogDebug("Unhandled Connect: " + tp + ", status is " + m_status + " length: " + payloadLength);
					break;
				case NetMessageType.ConnectResponse:
					switch (m_status)
					{
						case NetConnectionStatus.InitiatedConnect:
							// awesome
							bool ok = ValidateHandshakeData(ptr, payloadLength, out hail);
							if (ok)
							{
								if (hail != null)
								{
									m_remoteHailMessage = m_peer.CreateIncomingMessage(NetIncomingMessageType.Data, hail);
									m_remoteHailMessage.LengthBits = (hail.Length * 8);
								}
								else
								{
									m_remoteHailMessage = null;
								}

								m_peer.AcceptConnection(this);
								SendConnectionEstablished();
								return;
							}
							break;
						case NetConnectionStatus.RespondedConnect:
							// hello, wtf?
							break;
						case NetConnectionStatus.Disconnecting:
						case NetConnectionStatus.Disconnected:
						case NetConnectionStatus.None:
							// wtf? anyway, bye!
							break;
						case NetConnectionStatus.Connected:
							// my ConnectionEstablished must have been lost, send another one
							SendConnectionEstablished();
							return;
					}
					break;
				case NetMessageType.ConnectionEstablished:
					switch (m_status)
					{
						case NetConnectionStatus.Connected:
							// ok...
							break;
						case NetConnectionStatus.Disconnected:
						case NetConnectionStatus.Disconnecting:
						case NetConnectionStatus.None:
							// too bad, almost made it
							break;
						case NetConnectionStatus.InitiatedConnect:
							// weird, should have been ConnectResponse...
							break;
						case NetConnectionStatus.RespondedConnect:
							// awesome
				
							NetIncomingMessage msg = m_peer.SetupReadHelperMessage(ptr, payloadLength);
							InitializeRemoteTimeOffset(msg.ReadSingle());

							m_peer.AcceptConnection(this);
							InitializePing();
							SetStatus(NetConnectionStatus.Connected, "Connected to " + NetUtility.ToHexString(m_remoteUniqueIdentifier));
							return;
					}
					break;
				case NetMessageType.Disconnect:
					// ouch
					string reason = "Ouch";
					try
					{
						NetIncomingMessage inc = m_peer.SetupReadHelperMessage(ptr, payloadLength);
						reason = inc.ReadString();
					}
					catch
					{
					}
					SetStatus(NetConnectionStatus.Disconnected, reason);
					break;
				default:
					m_peer.LogDebug("Unhandled type during handshake: " + tp + " length: " + payloadLength);
					break;
			}
		}

		private bool ValidateHandshakeData(int ptr, int payloadLength, out byte[] hail)
		{
			hail = null;

			// create temporary incoming message
			NetIncomingMessage msg = m_peer.SetupReadHelperMessage(ptr, payloadLength);
			try
			{
				string remoteAppIdentifier = msg.ReadString();
				long remoteUniqueIdentifier = msg.ReadInt64();
				InitializeRemoteTimeOffset(msg.ReadSingle());

				int remainingBytes = payloadLength - (msg.PositionInBytes - ptr);
				if (remainingBytes > 0)
					hail = msg.ReadBytes(remainingBytes);

				if (remoteAppIdentifier != m_peer.m_configuration.AppIdentifier)
				{
					// wrong app identifier
					ExecuteDisconnect("Wrong application identifier!", true);
					return false;
				}

				m_remoteUniqueIdentifier = remoteUniqueIdentifier;
			}
			catch(Exception ex)
			{
				// whatever; we failed
				ExecuteDisconnect("Handshake data validation failed", true);
				m_peer.LogWarning("ReadRemoteHandshakeData failed: " + ex.Message);
				return false;
			}
			return true;
		}
		
		/// <summary>
		/// Disconnect from the remote peer
		/// </summary>
		/// <param name="byeMessage">the message to send with the disconnect message</param>
		public void Disconnect(string byeMessage)
		{
			// user or library thread
			if (m_status == NetConnectionStatus.None || m_status == NetConnectionStatus.Disconnected)
				return;

			m_peer.LogVerbose("Disconnect requested for " + this);
			m_disconnectMessage = byeMessage;

			if (m_status != NetConnectionStatus.Disconnected && m_status != NetConnectionStatus.None)
				SetStatus(NetConnectionStatus.Disconnecting, byeMessage);

			m_disconnectRequested = true;
		}
	}
}
