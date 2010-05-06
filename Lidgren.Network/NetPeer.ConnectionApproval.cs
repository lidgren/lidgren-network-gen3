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
using System.Collections.Generic;
using System;

namespace Lidgren.Network
{
	internal enum PendingConnectionStatus
	{
		NotPending = 0,
		Pending,
		Approved,
		Denied,
	}

	public partial class NetPeer
	{
		private List<NetConnection> m_pendingConnections;

		private void AddPendingConnection(NetConnection conn, NetIncomingMessage approval)
		{
			if (m_pendingConnections == null)
				m_pendingConnections = new List<NetConnection>();
			m_pendingConnections.Add(conn);
			conn.m_pendingStatus = PendingConnectionStatus.Pending;

			if (approval == null)
				approval = CreateIncomingMessage(NetIncomingMessageType.ConnectionApproval, 0);
			approval.m_messageType = NetMessageType.Library;
			approval.m_senderConnection = conn;
			approval.m_senderEndpoint = conn.m_remoteEndpoint;

			ReleaseMessage(approval);
		}

		private void CheckPendingConnections()
		{
			if (m_pendingConnections == null || m_pendingConnections.Count < 1)
				return;

			foreach (NetConnection conn in m_pendingConnections)
			{
				switch (conn.m_pendingStatus)
				{
					case PendingConnectionStatus.Pending:
						if (NetTime.Now > conn.m_connectInitationTime + 10.0)
						{
							LogWarning("Pending connection still in pending state after 10 seconds; forgot to Approve/Deny?");
							m_pendingConnections.Remove(conn);
							return;
						}
						break;
					case PendingConnectionStatus.Approved:
						// accept connection
						AcceptConnection(conn);
						m_pendingConnections.Remove(conn);
						return;
					case PendingConnectionStatus.Denied:
						// send disconnected
						NetOutgoingMessage bye = CreateLibraryMessage(NetMessageLibraryType.Disconnect, conn.m_pendingDenialReason);
						EnqueueUnconnectedMessage(bye, conn.m_remoteEndpoint);
						m_pendingConnections.Remove(conn);
						return;
				}
			}
		}
	}
}
