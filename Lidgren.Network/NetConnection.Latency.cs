using System;

namespace Lidgren.Network
{
	public partial class NetConnection
	{
		private float m_sentPingTime;
		private int m_sentPingNumber;
		private float m_averageRoundtripTime;
		private float m_timeoutDeadline = float.MaxValue;

		/// <summary>
		/// Gets the current average roundtrip time in seconds
		/// </summary>
		public float AverageRoundtripTime { get { return m_averageRoundtripTime; } }

		internal void InitializePing()
		{
			// randomize ping sent time (0.25 - 1.0 x ping interval)
			m_sentPingTime = (float)NetTime.Now;
			m_sentPingTime -= (m_peerConfiguration.PingInterval * 0.25f); // delay ping for a little while
			m_sentPingTime -= (NetRandom.Instance.NextSingle() * (m_peerConfiguration.PingInterval * 0.75f));
		}

		internal void SendPing()
		{
			m_peer.VerifyNetworkThread();

			m_sentPingNumber++;
			if (m_sentPingNumber >= 256)
				m_sentPingNumber = 0;
			m_sentPingTime = (float)NetTime.Now;
			NetOutgoingMessage om = m_peer.CreateMessage(1);
			om.Write((byte)m_sentPingNumber);
			om.m_messageType = NetMessageType.Ping;

			int len = om.Encode(m_peer.m_sendBuffer, 0, 0);
			bool connectionReset;
			m_peer.SendPacket(len, m_remoteEndpoint, 1, out connectionReset);

			m_statistics.PacketSent(len, 1);
		}

		internal void SendPong(int pingNumber)
		{
			m_peer.VerifyNetworkThread();

			NetOutgoingMessage om = m_peer.CreateMessage(1);
			om.Write((byte)pingNumber);
			om.m_messageType = NetMessageType.Pong;

			int len = om.Encode(m_peer.m_sendBuffer, 0, 0);
			bool connectionReset;
			m_peer.SendPacket(len, m_remoteEndpoint, 1, out connectionReset);

			m_statistics.PacketSent(len, 1);
		}

		internal void ReceivedPong(float now, int pongNumber)
		{
			if (pongNumber != m_sentPingNumber)
			{
				m_peer.LogVerbose("Ping/Pong mismatch; dropped message?");
				return;
			}

			m_timeoutDeadline = now + m_peerConfiguration.m_connectionTimeout;

			float rtt = now - m_sentPingTime;
			NetException.Assert(rtt >= 0);

			if (m_averageRoundtripTime < 0)
			{
				m_averageRoundtripTime = rtt; // initial estimate
				m_peer.LogDebug("Initiated average roundtrip time to " + NetTime.ToReadable(m_averageRoundtripTime));
			}
			else
			{
				m_averageRoundtripTime = (m_averageRoundtripTime * 0.7f) + (float)(rtt * 0.3f);
				m_peer.LogVerbose("Updated average roundtrip time to " + NetTime.ToReadable(m_averageRoundtripTime));
			}

			// update resend delay for all channels
			float resendDelay = GetResendDelay();
			foreach (var chan in m_sendChannels)
			{
				var rchan = chan as NetReliableSenderChannel;
				if (rchan != null)
					rchan.m_resendDelay = resendDelay;
			}

			m_peer.LogVerbose("Timeout deadline pushed to  " + m_timeoutDeadline);
		}
	}
}
