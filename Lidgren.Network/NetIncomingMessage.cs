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
using System.Diagnostics;
using System.Net;

namespace Lidgren.Network
{
	internal enum NetIncomingMessageReleaseStatus
	{
		NotReleased = 0,
		ReleasedToApplication,
		RecycledByApplication
	}

	[DebuggerDisplay("{m_readPosition} of {m_bitLength} bits ({LengthBytes} bytes) read")]
	public partial class NetIncomingMessage
	{
		internal byte[] m_data;
		internal int m_bitLength;
		internal NetMessageType m_messageType; // NetDeliveryMethod and sequence channel can be derived from this
		internal ushort m_sequenceNumber;
		internal NetIncomingMessageReleaseStatus m_status;

		internal NetIncomingMessageType m_incomingType;
		internal IPEndPoint m_senderEndpoint;
		internal NetConnection m_senderConnection;

		internal NetFragmentationInfo m_fragmentationInfo;

		/// <summary>
		/// Gets the length of the data in number of bytes
		/// </summary>
		public int LengthBytes
		{
			get { return ((m_bitLength + 7) >> 3); }
		}

		/// <summary>
		/// Gets the length of the data in number of bits
		/// </summary>
		public int LengthBits
		{
			get { return m_bitLength; }
		}

		/// <summary>
		/// Returns the internal data buffer, don't modify
		/// </summary>
		public byte[] PeekDataBuffer()
		{
			return m_data;
		}

		/// <summary>
		/// Gets the NetDeliveryMethod used by this message 
		/// </summary>
		public NetDeliveryMethod DeliveryMethod
		{
			get { return NetPeer.GetDeliveryMethod(m_messageType); }
		}

		/// <summary>
		/// Gets which sequence channel this message was sent in
		/// </summary>
		public int SequenceChannel
		{
			get { return (int)m_messageType - (int)NetPeer.GetDeliveryMethod(m_messageType); }
		}

		/// <summary>
		/// Type of data contained in this message
		/// </summary>
		public NetIncomingMessageType MessageType { get { return m_incomingType; } }

		/// <summary>
		/// IPEndPoint of sender, if any
		/// </summary>
		public IPEndPoint SenderEndpoint { get { return m_senderEndpoint; } }

		/// <summary>
		/// NetConnection of sender, if any
		/// </summary>
		public NetConnection SenderConnection { get { return m_senderConnection; } }

		internal NetIncomingMessage()
		{
		}

		internal NetIncomingMessage(byte[] data, int dataLength)
		{
			m_data = data;
			m_bitLength = dataLength * 8;
		}

		internal void Reset()
		{
			m_bitLength = 0;
			m_readPosition = 0;
			m_status = NetIncomingMessageReleaseStatus.NotReleased;
			m_fragmentationInfo = null;
		}

		public void Decrypt(NetXtea tea)
		{
			// requires blocks of 8 bytes
			int blocks = m_bitLength / 64;
			if (blocks * 64 != m_bitLength)
				throw new NetException("Wrong message length for XTEA decrypt! Length is " + m_bitLength + " bits");

			byte[] result = new byte[m_data.Length];
			for (int i = 0; i < blocks; i++)
				tea.DecryptBlock(m_data, (i * 8), result, (i * 8));
			m_data = result;
		}

		public NetIncomingMessage Clone()
		{
			NetIncomingMessage retval = new NetIncomingMessage();

			// copy content
			retval.m_data = new byte[LengthBytes];
			Buffer.BlockCopy(m_data, 0, retval.m_data, 0, LengthBytes);

			retval.m_bitLength = m_bitLength;
			retval.m_messageType = m_messageType;
			retval.m_sequenceNumber = m_sequenceNumber;
			retval.m_status = m_status;
			retval.m_incomingType = m_incomingType;
			retval.m_senderEndpoint = m_senderEndpoint;
			retval.m_senderConnection = m_senderConnection;
			retval.m_fragmentationInfo = m_fragmentationInfo;

			return retval;
		}

		public override string ToString()
		{
			return String.Format("[NetIncomingMessage {0}, {1}|{2}, {3} bits]",
				m_incomingType,
				m_messageType,
				m_sequenceNumber,
				m_bitLength
			);
		}
	}
}
