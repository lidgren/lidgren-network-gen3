using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;

namespace Lidgren.Network
{
	[DebuggerDisplay("MessageType={MessageType} SequenceNumber={SequenceNumber} NumSends={NumSends}")]
	internal sealed class NetSending
	{
		public NetOutgoingMessage Message;
		public IPEndPoint Recipient;
		public NetMessageType MessageType;
		public ushort SequenceNumber;
		public double NextResend;
		public int NumSends; // how many times has this sending been sent

		public int FragmentGroupId;
		public int FragmentNumber;
		public int FragmentTotalCount;

		public NetSending(NetOutgoingMessage msg, NetMessageType tp, ushort sequenceNumber)
		{
			Message = msg;
			MessageType = tp;
			SequenceNumber = sequenceNumber;
		}

		internal void SetNextResend(NetConnection conn)
		{
			float baseDelay;
			switch(NumSends)
			{
				case 0: baseDelay = 0.025f; break;
				case 1: baseDelay = 0.05f; break;
				case 2: baseDelay = 0.15f; break;
				case 3: baseDelay = 0.3f; break;
				default:
					baseDelay = (float)(NumSends - 3); // 4: 1 second, 5: 2 seconds, 6: 3 seconds etc
					break;
			}

			float rttMultiplier = 1.15f + (0.15f * NumSends);

			float totalDelay = baseDelay + (conn.AverageRoundtripTime * rttMultiplier);

			NextResend = NetTime.Now + totalDelay;
		}

		public override string ToString()
		{
			return "[NetSending " + MessageType + "#" + SequenceNumber + " NumSends: " + NumSends + "]";
		}
	}
}
