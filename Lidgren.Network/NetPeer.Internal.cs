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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		private EndPoint m_senderRemote;
		internal byte[] m_receiveBuffer;
		internal byte[] m_sendBuffer;
		internal Socket m_socket;
		internal byte[] m_macAddressBytes;
		private int m_listenPort;
		private AutoResetEvent m_messageReceivedEvent;

		private NetQueue<NetIncomingMessage> m_releasedIncomingMessages;
		private NetQueue<NetOutgoingMessage> m_unsentUnconnectedMessage;

		/// <summary>
		/// Signalling event which can be waited on to determine when a message is queued for reading.
		/// Note that there is no guarantee that after the event is signaled the blocked thread will 
		/// find the message in the queue. Other user created threads could be preempted and dequeue 
		/// the message before the waiting thread wakes up.
		/// </summary>
		public AutoResetEvent MessageReceivedEvent { get { return m_messageReceivedEvent; } }

		private void InternalInitialize()
		{
			m_releasedIncomingMessages = new NetQueue<NetIncomingMessage>(16);
			m_unsentUnconnectedMessage = new NetQueue<NetOutgoingMessage>(4);
			m_messageReceivedEvent = new AutoResetEvent(false);
		}

		internal void ReleaseMessage(NetIncomingMessage msg)
		{
			NetException.Assert(msg.m_status != NetIncomingMessageReleaseStatus.ReleasedToApplication, "Message released to application twice!");

			msg.m_status = NetIncomingMessageReleaseStatus.ReleasedToApplication;
			m_releasedIncomingMessages.Enqueue(msg);
			if (m_messageReceivedEvent != null)
				m_messageReceivedEvent.Set();
		}

		[System.Diagnostics.Conditional("DEBUG")]
		internal void VerifyNetworkThread()
		{
			Thread ct = System.Threading.Thread.CurrentThread;
			if (ct != m_networkThread)
				throw new NetException("Executing on wrong thread! Should be library system thread (is " + ct.Name + " mId " + ct.ManagedThreadId + ")");
		}

		//
		// Network loop
		//
		private void Run()
		{
			//
			// Initialize
			//
			VerifyNetworkThread();

			InitializeRecycling();

			System.Net.NetworkInformation.PhysicalAddress pa = NetUtility.GetMacAddress();
			if (pa != null)
			{
				m_macAddressBytes = pa.GetAddressBytes();
				LogVerbose("Mac address is " + NetUtility.ToHexString(m_macAddressBytes));
			}
			else
			{
				LogWarning("Failed to get Mac address");
			}
			
			LogDebug("Network thread started");

			lock (m_initializeLock)
			{
				if (m_status == NetPeerStatus.Running)
					return;

				m_statistics.Reset();

				// bind to socket
				IPEndPoint iep = null;
				try
				{
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

					long first = (pa == null ? (long)0 : (long)pa.GetHashCode());
					long second = (long)((long)boundEp.GetHashCode() << 32);
					m_uniqueIdentifier = first ^ second;

					m_receiveBuffer = new byte[m_configuration.ReceiveBufferSize];
					m_sendBuffer = new byte[m_configuration.SendBufferSize];

					LogVerbose("Initialization done");

					// only set Running if everything succeeds
					m_status = NetPeerStatus.Running;
				}
				catch (SocketException sex)
				{
					if (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
						throw new NetException("Failed to bind to port " + (iep == null ? "Null" : iep.ToString()) + " - Address already in use!", sex);
					throw;
				}
				catch (Exception ex)
				{
					throw new NetException("Failed to bind to " + (iep == null ? "Null" : iep.ToString()), ex);
				}
			}

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
			LogDebug("Shutting down...");

			// disconnect and make one final heartbeat
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
					if (conn.m_status == NetConnectionStatus.Connected || conn.m_status == NetConnectionStatus.Connecting)
						conn.Disconnect(m_shutdownReason);
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
						m_messageReceivedEvent.Close();
				}
				finally
				{
					m_socket = null;
					m_messageReceivedEvent = null;
					m_status = NetPeerStatus.NotRunning;
					LogDebug("Shutdown complete");
				}
			}

			return;
		}

		private void Heartbeat()
		{
			VerifyNetworkThread();

#if DEBUG
			// send delayed packets
			SendDelayedPackets();
#endif

			// connection approval
			CheckPendingConnections();

			double now = NetTime.Now;
			
			// do connection heartbeats
			foreach (NetConnection conn in m_connections)
			{
				conn.Heartbeat(now);
				if (conn.m_status == NetConnectionStatus.Disconnected)
				{
					RemoveConnection(conn);
					break; // can't continue iteration here
				}
			}

			// send unconnected sends
			NetOutgoingMessage um;
			while ((um = m_unsentUnconnectedMessage.TryDequeue()) != null)
			{
				IPEndPoint recipient = um.m_unconnectedRecipient;

				//
				// TODO: use throttling here
				//

				int ptr = um.Encode(m_sendBuffer, 0, null);

				if (recipient.Address.Equals(IPAddress.Broadcast))
				{
					// send using broadcast
					try
					{
						m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
						SendPacket(ptr, recipient, 1);
					}
					finally
					{
						m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
					}
				}
				else
				{
					// send normally
					SendPacket(ptr, recipient, 1);
				}
			}

			// check if we need to reduce the recycled pool
			ReduceStoragePool();

			//
			// read from socket
			//
			do
			{
				if (m_socket == null)
					return;

				if (!m_socket.Poll(1000, SelectMode.SelectRead)) // wait up to 1 ms for data to arrive
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
					// no good response to this yet
					if (sx.ErrorCode == 10054)
					{
						// connection reset by peer, aka forcibly closed
						// we should shut down the connection; but m_senderRemote seemingly cannot be trusted, so which connection should we shut down?!
						//LogWarning("Connection reset by peer, seemingly from " + m_senderRemote);
						return;
					}

					LogWarning(sx.ToString());
					return;
				}

				if (bytesReceived < 1)
					return;

				// renew current time; we might have waited in Poll
				now = NetTime.Now;

				//LogVerbose("Received " + bytesReceived + " bytes");

				IPEndPoint ipsender = (IPEndPoint)m_senderRemote;

				NetConnection sender = null;
				m_connectionLookup.TryGetValue(ipsender, out sender);

				int ptr = 0;
				NetMessageType msgType;
				NetMessageLibraryType libType = NetMessageLibraryType.Error;

				//
				// parse packet into messages
				//
				int numMessagesReceived = 0;
				while ((bytesReceived - ptr) >= NetPeer.kMinPacketHeaderSize)
				{
					// get NetMessageType
					byte top = m_receiveBuffer[ptr++];
					bool isFragment = (top & 128) == 128;
					msgType = (NetMessageType)(top & 127);

					// get NetmessageLibraryType?
					if (msgType == NetMessageType.Library)
						libType = (NetMessageLibraryType)m_receiveBuffer[ptr++];

					// get sequence number?
					ushort sequenceNumber;
					if (msgType >= NetMessageType.UserSequenced)
						sequenceNumber = (ushort)(m_receiveBuffer[ptr++] | (m_receiveBuffer[ptr++] << 8));
					else
						sequenceNumber = 0;

					// get payload length
					int payloadLengthBits = (int)m_receiveBuffer[ptr++];
					if ((payloadLengthBits & 128) == 128) // large payload
						payloadLengthBits = (payloadLengthBits & 127) | (m_receiveBuffer[ptr++] << 7);

					int payloadLengthBytes = NetUtility.BytesToHoldBits(payloadLengthBits);

					if ((ptr + payloadLengthBytes) > bytesReceived)
					{
						LogWarning("Malformed message from " + ipsender.ToString() + "; not enough bytes");
						break;
					}

					//
					// handle incoming message
					//

					if (msgType == NetMessageType.Error)
					{
						LogError("Malformed message; no message type!");
						continue;
					}

					numMessagesReceived++;

					if (msgType == NetMessageType.Library)
					{
						if (sender == null)
							HandleUnconnectedLibraryMessage(libType, ptr, payloadLengthBits, ipsender);
						else
							sender.HandleLibraryMessage(now, libType, ptr, payloadLengthBits);
					}
					else
					{
						if (sender == null)
						{
							if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.UnconnectedData))
								HandleUnconnectedUserMessage(ptr, payloadLengthBits, ipsender);
						}
						else
						{
							if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.Data))
								sender.HandleUserMessage(now, msgType, isFragment, sequenceNumber, ptr, payloadLengthBits);
						}
					}

					if (isFragment)
						ptr += NetConstants.FragmentHeaderSize;

					ptr += payloadLengthBytes;
				}

				m_statistics.PacketReceived(bytesReceived, numMessagesReceived);

				if (sender != null)
				{
					sender.m_lastHeardFrom = now;
					sender.m_statistics.PacketReceived(bytesReceived, numMessagesReceived);
				}

				if (ptr < bytesReceived)
				{
					// malformed packet
					LogWarning("Malformed packet from " + sender + " (" + ipsender + "); " + (ptr - bytesReceived) + " stray bytes");
					continue;
				}
			} while (true);

			// heartbeat done
			return;
		}

		private void HandleUnconnectedLibraryMessage(NetMessageLibraryType libType, int ptr, int payloadLengthBits, IPEndPoint senderEndpoint)
		{
			VerifyNetworkThread();

			if (libType != NetMessageLibraryType.Connect && libType != NetMessageLibraryType.Discovery && libType != NetMessageLibraryType.DiscoveryResponse)
			{
				LogWarning("Received unconnected library message of type " + libType);
				return;
			}

			int payloadLengthBytes = NetUtility.BytesToHoldBits(payloadLengthBits);

			//
			// Handle Discovery
			//
			if (libType == NetMessageLibraryType.Discovery)
			{
				if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.DiscoveryRequest))
				{
					NetIncomingMessage dm = CreateIncomingMessage(NetIncomingMessageType.DiscoveryRequest, payloadLengthBytes);
					if (payloadLengthBytes > 0)
						Buffer.BlockCopy(m_receiveBuffer, ptr, dm.m_data, 0, payloadLengthBytes);
					dm.m_bitLength = payloadLengthBits;
					dm.m_senderEndpoint = senderEndpoint;
					ReleaseMessage(dm);
				}
				return;
			}

			if (libType == NetMessageLibraryType.DiscoveryResponse)
			{
				if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.DiscoveryResponse))
				{
					NetIncomingMessage dr = CreateIncomingMessage(NetIncomingMessageType.DiscoveryResponse, payloadLengthBytes);
					if (payloadLengthBytes > 0)
						Buffer.BlockCopy(m_receiveBuffer, ptr, dr.m_data, 0, payloadLengthBytes);
					dr.m_bitLength = payloadLengthBits;
					dr.m_senderEndpoint = senderEndpoint;
					ReleaseMessage(dr);
				}
				return;
			}
			
			//
			// Handle NetMessageLibraryType.Connect
			//

			if (!m_configuration.m_acceptIncomingConnections)
			{
				LogWarning("Connect received; but we're not accepting incoming connections!");
				return;
			}

			string appIdent;
			long remoteUniqueIdentifier = 0;
			NetIncomingMessage approval = null;
			try
			{
				NetIncomingMessage reader = new NetIncomingMessage();

				reader.m_data = GetStorage(payloadLengthBytes);
				Buffer.BlockCopy(m_receiveBuffer, ptr, reader.m_data, 0, payloadLengthBytes);
				ptr += payloadLengthBytes;
				reader.m_bitLength = payloadLengthBits;
				appIdent = reader.ReadString();
				remoteUniqueIdentifier = reader.ReadInt64();

				int approvalBitLength = (int)reader.ReadVariableUInt32();
				if (approvalBitLength > 0)
				{
					int approvalByteLength = NetUtility.BytesToHoldBits(approvalBitLength);
					if (approvalByteLength < m_configuration.MaximumTransmissionUnit)
					{
						approval = CreateIncomingMessage(NetIncomingMessageType.ConnectionApproval, approvalByteLength);
						reader.ReadBits(approval.m_data, 0, approvalBitLength);
						approval.m_bitLength = approvalBitLength;
					}
				}
			}
			catch (Exception ex)
			{
				// malformed connect packet
				LogWarning("Malformed connect packet from " + senderEndpoint + " - " + ex.ToString());
				return;
			}

			if (appIdent.Equals(m_configuration.AppIdentifier) == false)
			{
				// wrong app ident
				LogWarning("Connect received with wrong appidentifier (need '" + m_configuration.AppIdentifier + "' found '" + appIdent + "') from " + senderEndpoint);
				return;
			}

			// ok, someone wants to connect to us, and we're accepting connections!
			if (m_connections.Count >= m_configuration.MaximumConnections)
			{
				HandleServerFull(senderEndpoint);
				return;
			}

			NetConnection conn = new NetConnection(this, senderEndpoint);
			conn.m_connectionInitiator = false;
			conn.m_connectInitationTime = NetTime.Now;
			conn.m_remoteUniqueIdentifier = remoteUniqueIdentifier;

			if (m_configuration.IsMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval))
			{
				// do connection approval before accepting this connection
				AddPendingConnection(conn, approval);
				return;
			}

			AcceptConnection(conn);
			return;
		}

		private void HandleUnconnectedUserMessage(int ptr, int payloadLengthBits, IPEndPoint senderEndpoint)
		{
			VerifyNetworkThread();

			NetIncomingMessage ium = CreateIncomingMessage(NetIncomingMessageType.UnconnectedData, m_receiveBuffer, ptr, NetUtility.BytesToHoldBits(payloadLengthBits));
			ium.m_bitLength = payloadLengthBits;
			ium.m_senderEndpoint = senderEndpoint;
			ReleaseMessage(ium);
		}

		private void AcceptConnection(NetConnection conn)
		{
			lock (m_connections)
			{
				m_connections.Add(conn);
				m_connectionLookup[conn.m_remoteEndpoint] = conn;
			}
			conn.SetStatus(NetConnectionStatus.Connecting, "Connecting");

			// send connection response
			conn.SendConnectResponse();

			conn.m_connectInitationTime = NetTime.Now;

			return;
		}

		internal void RemoveConnection(NetConnection conn)
		{
			lock (m_connections)
			{
				m_connections.Remove(conn);
				m_connectionLookup.Remove(conn.m_remoteEndpoint);
			}
			conn.Dispose();
		}

		private void HandleServerFull(IPEndPoint connecter)
		{
			const string rejectMessage = "Server is full!";
			NetOutgoingMessage reply = CreateLibraryMessage(NetMessageLibraryType.Disconnect, rejectMessage);
			EnqueueUnconnectedMessage(reply, connecter);
		}

		// called by user and network thread
		private void EnqueueUnconnectedMessage(NetOutgoingMessage msg, IPEndPoint recipient)
		{
			msg.m_unconnectedRecipient = recipient;
			Interlocked.Increment(ref msg.m_inQueueCount);
			m_unsentUnconnectedMessage.Enqueue(msg);
		}

		internal static NetDeliveryMethod GetDeliveryMethod(NetMessageType mtp)
		{
			if (mtp >= NetMessageType.UserReliableOrdered)
				return NetDeliveryMethod.ReliableOrdered;
			else if (mtp >= NetMessageType.UserReliableSequenced)
				return NetDeliveryMethod.ReliableSequenced;
			else if (mtp >= NetMessageType.UserReliableUnordered)
				return NetDeliveryMethod.ReliableUnordered;
			else if (mtp >= NetMessageType.UserSequenced)
				return NetDeliveryMethod.UnreliableSequenced;
			return NetDeliveryMethod.Unreliable;
		}

		internal void SendImmediately(NetConnection conn, NetOutgoingMessage msg)
		{
			NetException.Assert(msg.m_type == NetMessageType.Library, "SendImmediately can only send library (non-reliable) messages");

			msg.m_inQueueCount = 1;
			int len = msg.Encode(m_sendBuffer, 0, conn);
			Interlocked.Decrement(ref msg.m_inQueueCount);

			SendPacket(len, conn.m_remoteEndpoint, 1);

			Recycle(msg);
		}
	}
}