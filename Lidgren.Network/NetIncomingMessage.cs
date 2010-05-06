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
using System.Diagnostics;

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
			m_fragmentationInfo = null;
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
