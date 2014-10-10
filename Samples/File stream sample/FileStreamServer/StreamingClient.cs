using System;
using Lidgren.Network;
using System.IO;

namespace FileStreamServer
{
	public class StreamingClient
	{
		private FileStream m_inputStream;
		private int m_sentOffset;
		private int m_chunkLen;
		private byte[] m_tmpBuffer;
		private NetConnection m_connection;
		
		public StreamingClient(NetConnection conn, string fileName)
		{
			m_connection = conn;
			m_inputStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			m_chunkLen = m_connection.Peer.Configuration.MaximumTransmissionUnit - 20;
			m_tmpBuffer = new byte[m_chunkLen];
			m_sentOffset = 0;
		}

		public void Heartbeat()
		{
			if (m_inputStream == null)
				return;

			if (m_connection.CanSendImmediately(NetDeliveryMethod.ReliableOrdered, 1))
			{
				// send another part of the file!
				long remaining = m_inputStream.Length - m_sentOffset;
				int sendBytes = (remaining > m_chunkLen ? m_chunkLen : (int)remaining);

				// just assume we can read the whole thing in one Read()
				m_inputStream.Read(m_tmpBuffer, 0, sendBytes);

				NetOutgoingMessage om;
				if (m_sentOffset == 0)
				{
					// first message; send length, chunk length and file name
					om = m_connection.Peer.CreateMessage(sendBytes + 8);
					om.Write((ulong)m_inputStream.Length);
					om.Write(Path.GetFileName(m_inputStream.Name));
					m_connection.SendMessage(om, NetDeliveryMethod.ReliableOrdered, 1);
				}

				om = m_connection.Peer.CreateMessage(sendBytes + 8);
				om.Write(m_tmpBuffer, 0, sendBytes);

				m_connection.SendMessage(om, NetDeliveryMethod.ReliableOrdered, 1);
				m_sentOffset += sendBytes;

				//Program.Output("Sent " + m_sentOffset + "/" + m_inputStream.Length + " bytes to " + m_connection);

				if (remaining - sendBytes <= 0)
				{
					m_inputStream.Close();
					m_inputStream.Dispose();
					m_inputStream = null;
				}
			}
		}
	}
}
