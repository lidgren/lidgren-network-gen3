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
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Lidgren.Network
{
	//
	// This partial file holds public netpeer methods accessible to the application
	//
	[DebuggerDisplay("Status={m_status}")]
	public partial class NetPeer
	{
		internal const int kMinPacketHeaderSize = 2;
		internal const int kMaxPacketHeaderSize = 5;

		private NetPeerStatus m_status;
		private object m_initializeLock = new object();
		internal long m_uniqueIdentifier;

		internal NetPeerConfiguration m_configuration;
		internal NetPeerStatistics m_statistics;
		private Thread m_networkThread;
		private string m_shutdownReason;

		internal List<NetConnection> m_connections;
		private Dictionary<IPEndPoint, NetConnection> m_connectionLookup;

		/// <summary>
		/// Gets the status of the NetPeer
		/// </summary>
		public NetPeerStatus Status { get { return m_status; } }

		/// <summary>
		/// Gets a copy of the list of connections
		/// </summary>
		public NetConnection[] Connections
		{
			get
			{
				lock (m_connections)
					return m_connections.ToArray();
			}
		}

		/// <summary>
		/// Returns the number of active connections
		/// </summary>
		public int ConnectionsCount
		{
			get { return m_connections.Count; }
		}

		/// <summary>
		/// Statistics on this NetPeer since it was initialized
		/// </summary>
		public NetPeerStatistics Statistics
		{
			get { return m_statistics; }
		}

		/// <summary>
		/// Gets the configuration of the netpeer
		/// </summary>
		public NetPeerConfiguration Configuration { get { return m_configuration; } }

		/// <summary>
		/// Gets the port number this NetPeer is listening and sending on
		/// </summary>
		public int Port { get { return m_listenPort; } }

		/// <summary>
		/// Gets a semi-unique identifier based on Mac address and ip/port. Note! Not available until Start has been called!
		/// </summary>
		public long UniqueIdentifier { get { return m_uniqueIdentifier; } }

		public NetPeer(NetPeerConfiguration configuration)
		{
			m_status = NetPeerStatus.NotRunning;
			m_configuration = configuration;
			m_connections = new List<NetConnection>();
			m_connectionLookup = new Dictionary<IPEndPoint, NetConnection>();
			m_senderRemote = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
			m_statistics = new NetPeerStatistics(this);

			InternalInitialize();
		}

		/// <summary>
		/// Binds to socket
		/// </summary>
		public void Start()
		{
			if (m_status != NetPeerStatus.NotRunning)
			{
				// already running! Just ignore...
				LogWarning("Start() called on already running NetPeer - ignoring.");
				return;
			}

			m_status = NetPeerStatus.Starting;

			m_releasedIncomingMessages.Clear();
			m_unsentUnconnectedMessage.Clear();

			m_configuration.VerifyAndLock();

			// start network thread
			m_networkThread = new Thread(new ThreadStart(Run));
			m_networkThread.Name = "Lidgren network thread";
			m_networkThread.IsBackground = true;
			m_networkThread.Start();

			// allow some time for network thread to start up in case they call Connect() immediately
			Thread.Sleep(3);
		}

		/// <summary>
		/// Read a pending message from any connection, if any
		/// </summary>
		public NetIncomingMessage ReadMessage()
		{
			if (m_status == NetPeerStatus.NotRunning)
				return null;

			return m_releasedIncomingMessages.TryDequeue();
		}

		public NetIncomingMessage WaitMessage(int maxMillis)
		{
			if (m_messageReceivedEvent != null)
				m_messageReceivedEvent.WaitOne(maxMillis);
			return m_releasedIncomingMessages.TryDequeue();
		}

		/// <summary>
		/// Create a connection to a remote endpoint
		/// </summary>
		public NetConnection Connect(string host, int port)
		{
			return Connect(new IPEndPoint(NetUtility.Resolve(host), port), null);
		}

		/// <summary>
		/// Create a connection to a remote endpoint
		/// </summary>
		public NetConnection Connect(IPEndPoint remoteEndpoint)
		{
			return Connect(remoteEndpoint, null);
		}

		/// <summary>
		/// Create a connection to a remote endpoint
		/// </summary>
		public NetConnection Connect(string host, int port, NetOutgoingMessage approvalMessage)
		{
			return Connect(new IPEndPoint(NetUtility.Resolve(host), port), approvalMessage);
		}

		/// <summary>
		/// Create a connection to a remote endpoint
		/// </summary>
		public virtual NetConnection Connect(IPEndPoint remoteEndpoint, NetOutgoingMessage approvalMessage)
		{
			if (m_status == NetPeerStatus.NotRunning)
				throw new NetException("Must call Start() first");

			if (m_connectionLookup.ContainsKey(remoteEndpoint))
				throw new NetException("Already connected to that endpoint!");

			NetConnection conn = new NetConnection(this, remoteEndpoint);
			conn.m_approvalMessage = approvalMessage;

			// handle on network thread
			conn.m_connectRequested = true;
			conn.m_connectionInitiator = true;

			lock (m_connections)
			{
				m_connections.Add(conn);
				m_connectionLookup[remoteEndpoint] = conn;
			}

			return conn;
		}

		/// <summary>
		/// Send a message to an existing connection
		/// </summary>
		public bool SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod deliveryMethod)
		{
			return SendMessage(msg, recipient, deliveryMethod, 0);
		}

		/// <summary>
		/// Send a message to an existing connection
		/// </summary>
		public bool SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod deliveryMethod, int channel)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			if (channel < 0 || channel > 63)
				throw new NetException("Channel must be between 0 and 63");
			if (channel != 0 && (deliveryMethod == NetDeliveryMethod.Unreliable || deliveryMethod == NetDeliveryMethod.ReliableUnordered))
				throw new NetException("Channel must be 0 for Unreliable and ReliableUnordered");

			if (m_status != NetPeerStatus.Running)
				return false;

			msg.m_type = (NetMessageType)((int)deliveryMethod + channel);

			recipient.EnqueueOutgoingMessage(msg);

			return true;
		}

		/// <summary>
		/// Send a message to a number of existing connections
		/// </summary>
		public bool SendMessage(NetOutgoingMessage msg, IEnumerable<NetConnection> recipients, NetDeliveryMethod deliveryMethod, int channel)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			if (channel < 0 || channel > 63)
				throw new NetException("Channel must be between 0 and 63");
			if (channel != 0 && (deliveryMethod == NetDeliveryMethod.Unreliable || deliveryMethod == NetDeliveryMethod.ReliableUnordered))
				throw new NetException("Channel must be 0 for Unreliable and ReliableUnordered");

			if (m_status != NetPeerStatus.Running)
				return false;

			msg.m_type = (NetMessageType)((int)deliveryMethod + channel);

			foreach (NetConnection conn in recipients)
				conn.EnqueueOutgoingMessage(msg);

			return true;
		}

		/// <summary>
		/// Send a message to an unconnected host
		/// </summary>
		public void SendUnconnectedMessage(NetOutgoingMessage msg, string host, int port)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");

			IPAddress adr = NetUtility.Resolve(host);
			if (adr == null)
				throw new NetException("Failed to resolve " + host);

			msg.m_type = NetMessageType.UserUnreliable; // sortof not applicable
			EnqueueUnconnectedMessage(msg, new IPEndPoint(adr, port));
		}

		/// <summary>
		/// Send a message to an unconnected host
		/// </summary>
		public void SendUnconnectedMessage(NetOutgoingMessage msg, IPEndPoint recipient)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			msg.m_type = NetMessageType.UserUnreliable; // sortof not applicable
			EnqueueUnconnectedMessage(msg, recipient);
		}

		internal void SendUnconnectedLibraryMessage(NetOutgoingMessage msg, NetMessageLibraryType libType, IPEndPoint recipient)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			msg.m_type = NetMessageType.Library;
			msg.m_libType = libType;
			EnqueueUnconnectedMessage(msg, recipient);
		}

		/// <summary>
		/// Send a message to a number of unconnected hosts
		/// </summary>
		public void SendUnconnectedMessage(NetOutgoingMessage msg, IEnumerable<IPEndPoint> recipients)
		{
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			msg.m_type = NetMessageType.UserUnreliable; // sortof not applicable
			foreach (IPEndPoint ipe in recipients)
				EnqueueUnconnectedMessage(msg, ipe);
		}

		/// <summary>
		/// Send a discovery response message
		/// </summary>
		public void SendDiscoveryResponse(NetOutgoingMessage msg, IPEndPoint recipient)
		{
			if (msg == null)
				msg = CreateMessage(0);
			if (msg.IsSent)
				throw new NetException("Message has already been sent!");
			msg.m_type = NetMessageType.Library;
			msg.m_libType = NetMessageLibraryType.DiscoveryResponse;
			EnqueueUnconnectedMessage(msg, recipient);
		}

		/// <summary>
		/// Disconnects all active connections and closes the socket
		/// </summary>
		public void Shutdown(string bye)
		{
			// called on user thread

			if (m_socket == null)
				return; // already shut down

			LogDebug("Shutdown requested");
			m_shutdownReason = bye;
			m_status = NetPeerStatus.ShutdownRequested;
		}

		public override string ToString()
		{
			return "[NetPeer bound to " + m_socket.LocalEndPoint + " " + ConnectionsCount + " connections]";
		}
	}
}
