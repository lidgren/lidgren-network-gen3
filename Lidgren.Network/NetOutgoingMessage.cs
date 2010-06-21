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
using System.Text;

namespace Lidgren.Network
{
	[DebuggerDisplay("LengthBits={LengthBits}")]
	public sealed partial class NetOutgoingMessage
	{
		// reference count before message can be recycled
		internal int m_numUnfinishedSendings;

		internal bool m_wasSent; // true is SendMessage() public method has been called
		internal NetMessageLibraryType m_libType = NetMessageLibraryType.Error;

		//internal int m_fragmentGroupId;
		//internal int m_fragmentNumber;
		//internal int m_fragmentTotalCount;

		/// <summary>
		/// Returns true if this message has been passed to SendMessage() already
		/// </summary>
		public bool IsSent { get { return m_wasSent; } }

		internal NetOutgoingMessage()
		{
		}

		internal void Reset()
		{
			NetException.Assert(m_numUnfinishedSendings == 0, "Ouch! Resetting NetOutgoingMessage still in some queue!");
			NetException.Assert(m_wasSent == true, "Ouch! Resetting unsent message!");

			m_bitLength = 0;
			m_libType = NetMessageLibraryType.Error;
			m_wasSent = false;
		}

		internal static int EncodeAcksMessage(byte[] buffer, int ptr, NetConnection conn, int maxBytesPayload)
		{
			// TODO: if appropriate; make bit vector of adjacent acks

			buffer[ptr++] = (byte)NetMessageType.Library;
			buffer[ptr++] = (byte)NetMessageLibraryType.Acknowledge;

			Queue<int> acks = conn.m_acknowledgesToSend;

			int maxAcks = maxBytesPayload / 3;
			int acksToEncode = (acks.Count < maxAcks ? acks.Count : maxAcks);

			int payloadBitsLength = acksToEncode * 3 * 8;
			if (payloadBitsLength < 127)
			{
				buffer[ptr++] = (byte)payloadBitsLength;
			}
			else
			{
				buffer[ptr++] = (byte)((payloadBitsLength & 127) | 128);
				buffer[ptr++] = (byte)(payloadBitsLength >> 7);
			}

			for (int i = 0; i < acksToEncode; i++)
			{
				int ack = acks.Dequeue();
				buffer[ptr++] = (byte)ack; // message type
				buffer[ptr++] = (byte)(ack >> 8); // seqnr low
				buffer[ptr++] = (byte)(ack >> 16); // seqnr high
			}

			return ptr;
		}

		internal int EncodeUnfragmented(byte[] buffer, int ptr, NetMessageType tp, ushort sequenceNumber)
		{
			// message type
			buffer[ptr++] = (byte)tp; //  | (m_fragmentGroupId == -1 ? 0 : 128));

			if (tp == NetMessageType.Library)
				buffer[ptr++] = (byte)m_libType;

			// channel sequence number
			if (tp >= NetMessageType.UserSequenced)
			{
				ushort seqNr = (ushort)sequenceNumber;
				buffer[ptr++] = (byte)seqNr;
				buffer[ptr++] = (byte)(seqNr >> 8);
			}

			// payload length
			int payloadBitsLength = LengthBits;
			int payloadBytesLength = NetUtility.BytesToHoldBits(payloadBitsLength);
			if (payloadBitsLength < 127)
			{
				buffer[ptr++] = (byte)payloadBitsLength;
			}
			else if (payloadBitsLength < 32768)
			{
				buffer[ptr++] = (byte)((payloadBitsLength & 127) | 128);
				buffer[ptr++] = (byte)(payloadBitsLength >> 7);
			}
			else
			{
				throw new NetException("Packet content too large; 4095 bytes maximum");
			}

			// payload
			if (payloadBitsLength > 0)
			{
				// zero out last byte
				buffer[ptr + payloadBytesLength] = 0;

				Buffer.BlockCopy(m_data, 0, buffer, ptr, payloadBytesLength);
				ptr += payloadBytesLength;
			}

			return ptr;
		}

		internal int EncodeFragmented(byte[] buffer, int ptr, NetSending send, int mtu)
		{
			NetException.Assert(send.MessageType != NetMessageType.Library, "Library messages cant be fragmented");

			// message type
			buffer[ptr++] = (byte)((int)send.MessageType | 128);

			// channel sequence number
			if (send.MessageType >= NetMessageType.UserSequenced)
			{
				ushort seqNr = (ushort)send.SequenceNumber;
				buffer[ptr++] = (byte)seqNr;
				buffer[ptr++] = (byte)(seqNr >> 8);
			}

			// calculate fragment payload length
			mtu -= NetConstants.FragmentHeaderSize; // size of fragmentation info
			int thisFragmentLength = (send.FragmentNumber == send.FragmentTotalCount - 1 ? (send.Message.LengthBytes - (mtu * (send.FragmentTotalCount - 1))) : mtu);

			int payloadBitsLength = thisFragmentLength * 8;
			if (payloadBitsLength < 127)
			{
				buffer[ptr++] = (byte)payloadBitsLength;
			}
			else if (payloadBitsLength < 32768)
			{
				buffer[ptr++] = (byte)((payloadBitsLength & 127) | 128);
				buffer[ptr++] = (byte)(payloadBitsLength >> 7);
			}
			else
			{
				throw new NetException("Packet content too large; 4095 bytes maximum");
			}

			// fragmentation info
			buffer[ptr++] = (byte)send.FragmentGroupId;
			buffer[ptr++] = (byte)(send.FragmentGroupId >> 8);
			buffer[ptr++] = (byte)send.FragmentTotalCount;
			buffer[ptr++] = (byte)(send.FragmentTotalCount >> 8);
			buffer[ptr++] = (byte)send.FragmentNumber;
			buffer[ptr++] = (byte)(send.FragmentNumber >> 8);

			// payload
			if (payloadBitsLength > 0)
			{
				// zero out last byte
				buffer[ptr + thisFragmentLength] = 0;

				int offset = (mtu * send.FragmentNumber);

				Buffer.BlockCopy(m_data, offset, buffer, ptr, thisFragmentLength);
				ptr += thisFragmentLength;
			}

			return ptr;
		}

		public void Encrypt(NetXtea tea)
		{
			// need blocks of 8 bytes
			WritePadBits();
			int blocksNeeded = (m_bitLength + 63) / 64;
			int missingBits = (blocksNeeded * 64) - m_bitLength;
			int missingBytes = NetUtility.BytesToHoldBits(missingBits);
			for (int i = 0; i < missingBytes; i++)
				Write((byte)0);

			byte[] result = new byte[m_data.Length];
			for(int i=0;i<blocksNeeded;i++)
				tea.EncryptBlock(m_data, (i * 8), result, (i * 8));
			m_data = result;
		}

		public override string ToString()
		{
			return "[NetOutgoingMessage " + LengthBytes + " bytes]";
		}
	}
}
