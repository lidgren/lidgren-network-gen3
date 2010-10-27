#define IS_MAC_AVAILABLE

using System;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		private NetPeerStatus m_status;
		private Thread m_networkThread;
		private Socket m_socket;
		internal byte[] m_sendBuffer;
		internal byte[] m_receiveBuffer;
		internal NetIncomingMessage m_readHelperMessage;
		private EndPoint m_senderRemote;
		private object m_initializeLock = new object();

		internal readonly NetPeerConfiguration m_configuration;
		private readonly NetQueue<NetIncomingMessage> m_releasedIncomingMessages;
		internal readonly NetQueue<NetTuple<IPEndPoint, NetOutgoingMessage>> m_unsentUnconnectedMessages;

		internal Dictionary<IPEndPoint, NetConnection> m_handshakes;

		internal readonly NetPeerStatistics m_statistics;
		internal long m_uniqueIdentifier;

		private AutoResetEvent m_messageReceivedEvent = new AutoResetEvent(false);

		internal void ReleaseMessage(NetIncomingMessage msg)
		{
			NetException.Assert(msg.m_incomingMessageType != NetIncomingMessageType.Error);

			if (msg.m_isFragment)
			{
				HandleReleasedFragment(msg);
				return;
			}
			
			m_releasedIncomingMessages.Enqueue(msg);
			if (m_messageReceivedEvent != null)
				m_messageReceivedEvent.Set();
		}

		private void InitializeNetwork()
		{
			lock (m_initializeLock)
			{
				m_configuration.Lock();

				if (m_status == NetPeerStatus.Running)
					return;

				InitializePools();

				m_releasedIncomingMessages.Clear();
				m_unsentUnconnectedMessages.Clear();
				m_handshakes.Clear();

				// bind to socket
				IPEndPoint iep = null;

				iep = new IPEndPoint(m_configuration.LocalAddress, m_configuration.Port);
				EndPoint ep = (EndPoint)iep;

				m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				m_socket.ReceiveBufferSize = m_configuration.ReceiveBufferSize;
				m_socket.SendBufferSize = m_configuration.SendBufferSize;
				m_socket.Blocking = false;
				m_socket.Bind(ep);

				IPEndPoint boundEp = m_socket.LocalEndPoint as IPEndPoint;
				LogDebug("Socket bound to " + boundEp + ": " + m_socket.IsBound);
				m_listenPort = boundEp.Port;

				m_receiveBuffer = new byte[m_configuration.ReceiveBufferSize];
				m_sendBuffer = new byte[m_configuration.SendBufferSize];
				m_readHelperMessage = new NetIncomingMessage(NetIncomingMessageType.Error);
				m_readHelperMessage.m_data = m_receiveBuffer;

				byte[] macBytes = new byte[8];
				NetRandom.Instance.NextBytes(macBytes);

#if IS_MAC_AVAILABLE
			System.Net.NetworkInformation.PhysicalAddress pa = NetUtility.GetMacAddress();
			if (pa != null)
			{
				macBytes = pa.GetAddressBytes();
				LogVerbose("Mac address is " + NetUtility.ToHexString(macBytes));
			}
			else
			{
				LogWarning("Failed to get Mac address");
			}
#endif
				byte[] epBytes = BitConverter.GetBytes(boundEp.GetHashCode());
				byte[] combined = new byte[epBytes.Length + macBytes.Length];
				Array.Copy(epBytes, 0, combined, 0, epBytes.Length);
				Array.Copy(macBytes, 0, combined, epBytes.Length, macBytes.Length);
				m_uniqueIdentifier = BitConverter.ToInt64(SHA1.Create().ComputeHash(combined), 0);

				m_status = NetPeerStatus.Running;
			}
		}

		private void NetworkLoop()
		{
			VerifyNetworkThread();

			LogDebug("Network thread started");

			//
			// Network loop
			//
			do
			{
				try
				{
					Heartbeat();
				}
				catch (Exception ex)
				{
					LogWarning(ex.ToString());
				}
			} while (m_status == NetPeerStatus.Running);

			//
			// perform shutdown
			//
			ExecutePeerShutdown();
		}

		private void ExecutePeerShutdown()
		{
			VerifyNetworkThread();

			LogDebug("Shutting down...");

			// disconnect and make one final heartbeat
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
					conn.Shutdown(m_shutdownReason);
			}

			lock (m_handshakes)
			{
				foreach (NetConnection conn in m_handshakes.Values)
					conn.Shutdown(m_shutdownReason);
			}

			// one final heartbeat, will send stuff and do disconnect
			Heartbeat();

			lock (m_initializeLock)
			{
				try
				{
					if (m_socket != null)
					{
						m_socket.Shutdown(SocketShutdown.Receive);
						m_socket.Close(2); // 2 seconds timeout
					}
					if (m_messageReceivedEvent != null)
					{
						m_messageReceivedEvent.Close();
						m_messageReceivedEvent = null;
					}
				}
				finally
				{
					m_socket = null;
					m_status = NetPeerStatus.NotRunning;
					LogDebug("Shutdown complete");
				}

				m_receiveBuffer = null;
				m_sendBuffer = null;
				m_unsentUnconnectedMessages.Clear();
				m_connections.Clear();
				m_handshakes.Clear();
			}

			return;
		}

		private void Heartbeat()
		{
			VerifyNetworkThread();

			float now = (float)NetTime.Now;

			// do handshake heartbeats
			foreach (NetConnection conn in m_handshakes.Values)
			{
				conn.UnconnectedHeartbeat(now);
				if (conn.m_status == NetConnectionStatus.Connected || conn.m_status == NetConnectionStatus.Disconnected)
					break; // collection is modified
			}

#if DEBUG
			SendDelayedPackets();
#endif

			// do connection heartbeats
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
				{
					conn.Heartbeat(now);
					if (conn.m_status == NetConnectionStatus.Disconnected)
					{
						//
						// remove connection
						//
						m_connections.Remove(conn);
						m_connectionLookup.Remove(conn.RemoteEndpoint);
						break; // can't continue iteration here
					}
				}
			}

			// send unsent unconnected messages
			NetTuple<IPEndPoint, NetOutgoingMessage> unsent;
			while (m_unsentUnconnectedMessages.TryDequeue(out unsent))
			{
				NetOutgoingMessage om = unsent.Item2;

				bool connReset;
				int len = om.Encode(m_sendBuffer, 0, 0);
				SendPacket(len, unsent.Item1, 1, out connReset);

				Interlocked.Decrement(ref om.m_recyclingCount);
				if (om.m_recyclingCount <= 0)
					Recycle(om);
			}

			//
			// read from socket
			//
			if (m_socket == null)
				return;

			if (!m_socket.Poll(500, SelectMode.SelectRead)) // wait up to 1/2 ms for data to arrive
				return;

			//if (m_socket == null || m_socket.Available < 1)
			//	return;

			int bytesReceived = 0;
			try
			{
				bytesReceived = m_socket.ReceiveFrom(m_receiveBuffer, 0, m_receiveBuffer.Length, SocketFlags.None, ref m_senderRemote);
			}
			catch (SocketException sx)
			{
				if (sx.SocketErrorCode == SocketError.ConnectionReset)
				{
					// connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable" 
					// we should shut down the connection; but m_senderRemote seemingly cannot be trusted, so which connection should we shut down?!
					// So, what to do?
					return;
				}

				LogWarning(sx.ToString());
				return;
			}

			if (bytesReceived < NetConstants.HeaderByteSize)
				return;

			//LogVerbose("Received " + bytesReceived + " bytes");

			IPEndPoint ipsender = (IPEndPoint)m_senderRemote;

			NetConnection sender = null;
			m_connectionLookup.TryGetValue(ipsender, out sender);

			//
			// parse packet into messages
			//
			int ptr = 0;
			while ((bytesReceived - ptr) >= NetConstants.HeaderByteSize)
			{
				// decode header
				//  8 bits - NetMessageType
				//  1 bit  - Fragment?
				// 15 bits - Sequence number
				// 16 bits - Payload length in bits

				NetMessageType tp = (NetMessageType)m_receiveBuffer[ptr++];

				byte low = m_receiveBuffer[ptr++];
				byte high = m_receiveBuffer[ptr++];

				bool isFragment = ((low & 1) == 1);
				ushort sequenceNumber = (ushort)((low >> 1) | (((int)high) << 7));
				
				ushort payloadBitLength = (ushort)(m_receiveBuffer[ptr++] | (m_receiveBuffer[ptr++] << 8));
				int payloadByteLength = NetUtility.BytesToHoldBits(payloadBitLength);

				if (bytesReceived - ptr < payloadByteLength)
				{
					LogWarning("Malformed packet; stated payload length " + payloadByteLength + ", remaining bytes " + (bytesReceived - ptr));
					return;
				}

				try
				{
					NetException.Assert(tp < NetMessageType.Unused1 || tp > NetMessageType.Unused29);

					if (tp >= NetMessageType.LibraryError)
					{
						if (sender != null)
							sender.ReceivedLibraryMessage(tp, ptr, payloadByteLength);
						else
							ReceivedUnconnectedLibraryMessage(ipsender, tp, ptr, payloadByteLength);
					}
					else
					{
						if (sender == null && !m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData))
							return; // dropping unconnected message since it's not enabled

						NetIncomingMessage msg = CreateIncomingMessage(NetIncomingMessageType.Data, payloadByteLength);
						msg.m_isFragment = isFragment;
						msg.m_sequenceNumber = sequenceNumber;
						msg.m_receivedMessageType = tp;
						msg.m_senderConnection = sender;
						msg.m_senderEndpoint = ipsender;
						msg.m_bitLength = payloadBitLength;
						Buffer.BlockCopy(m_receiveBuffer, ptr, msg.m_data, 0, payloadByteLength);
						if (sender != null)
						{
							if (tp == NetMessageType.Unconnected)
							{
								// We're connected; but we can still send unconnected messages to this peer
								msg.m_incomingMessageType = NetIncomingMessageType.UnconnectedData;
								ReleaseMessage(msg);
							}
							else
							{
								// connected application (non-library) message
								sender.ReceivedMessage(msg);
							}
						}
						else
						{
							// at this point we know the message type is enabled
							// unconnected application (non-library) message
							msg.m_incomingMessageType = NetIncomingMessageType.UnconnectedData;
							ReleaseMessage(msg);
						}
					}
				}
				catch (Exception ex)
				{
					LogError("Packet parsing error: " + ex.Message);
				}
				ptr += payloadByteLength;
			}
		}

		private void ReceivedUnconnectedLibraryMessage(IPEndPoint senderEndpoint, NetMessageType tp, int ptr, int payloadByteLength)
		{
			NetConnection shake;
			if (m_handshakes.TryGetValue(senderEndpoint, out shake))
			{
				shake.ReceivedHandshake(tp, ptr, payloadByteLength);
				return;
			}

			//
			// Library message from a completely unknown sender; lets just accept Connect
			//
			switch (tp)
			{
				case NetMessageType.Discovery:
					if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.DiscoveryRequest))
					{
						NetIncomingMessage dm = CreateIncomingMessage(NetIncomingMessageType.DiscoveryRequest, payloadByteLength);
						if (payloadByteLength > 0)
							Buffer.BlockCopy(m_receiveBuffer, ptr, dm.m_data, 0, payloadByteLength);
						dm.m_bitLength = payloadByteLength * 8;
						dm.m_senderEndpoint = senderEndpoint;
						ReleaseMessage(dm);
					}
					return;

				case NetMessageType.DiscoveryResponse:
					if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.DiscoveryResponse))
					{
						NetIncomingMessage dr = CreateIncomingMessage(NetIncomingMessageType.DiscoveryResponse, payloadByteLength);
						if (payloadByteLength > 0)
							Buffer.BlockCopy(m_receiveBuffer, ptr, dr.m_data, 0, payloadByteLength);
						dr.m_bitLength = payloadByteLength * 8;
						dr.m_senderEndpoint = senderEndpoint;
						ReleaseMessage(dr);
					}
					return;
				case NetMessageType.NatIntroduction:
					HandleNatIntroduction(ptr);
					return;
				case NetMessageType.NatPunchMessage:
					HandleNatPunch(ptr, senderEndpoint);
					return;
				case NetMessageType.Connect:
					// proceed
					break;
				case NetMessageType.Disconnect:
					// this is probably ok
					LogVerbose("Received Disconnect from unconnected source: " + senderEndpoint);
					return;
				default:
					LogWarning("Received unhandled library message " + tp + " from " + senderEndpoint);
					return;
			}

			// It's someone wanting to shake hands with us!

			int reservedSlots = m_handshakes.Count + m_connections.Count;
			if (reservedSlots >= m_configuration.m_maximumConnections)
			{
				// server full
				NetOutgoingMessage full = CreateMessage("Server full");
				full.m_messageType = NetMessageType.Disconnect;
				SendLibrary(full, senderEndpoint);
				return;
			}

			// Ok, start handshake!
			NetConnection conn = new NetConnection(this, senderEndpoint);
			m_handshakes.Add(senderEndpoint, conn);
			conn.ReceivedHandshake(tp, ptr, payloadByteLength);

			return;
		}

		internal void AcceptConnection(NetConnection conn)
		{
			// LogDebug("Accepted connection " + conn);

			if (m_handshakes.Remove(conn.m_remoteEndpoint) == false)
				LogWarning("AcceptConnection called but m_handshakes did not contain it!");

			lock (m_connections)
			{
				if (m_connections.Contains(conn))
				{
					LogWarning("AcceptConnection called but m_connection already contains it!");
				}
				else
				{
					m_connections.Add(conn);
					m_connectionLookup.Add(conn.m_remoteEndpoint, conn);
				}
			}
		}

		[Conditional("DEBUG")]
		internal void VerifyNetworkThread()
		{
			Thread ct = Thread.CurrentThread;
			if (Thread.CurrentThread != m_networkThread)
				throw new NetException("Executing on wrong thread! Should be library system thread (is " + ct.Name + " mId " + ct.ManagedThreadId + ")");
		}

		internal NetIncomingMessage SetupReadHelperMessage(int ptr, int payloadLength)
		{
			VerifyNetworkThread();

			m_readHelperMessage.m_bitLength = (ptr + payloadLength) * 8;
			m_readHelperMessage.m_readPosition = (ptr * 8);
			return m_readHelperMessage;
		}
	}
}
