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
using System.Threading;

namespace Lidgren.Network
{
	public partial class NetConnection
	{
		private ushort[] m_nextSendSequenceNumber;
		private ushort[] m_lastReceivedSequenced;

		internal readonly Dictionary<ushort, NetOutgoingMessage>[] m_storedMessages = new Dictionary<ushort, NetOutgoingMessage>[NetConstants.NumReliableChannels];
		internal readonly Dictionary<NetOutgoingMessage, ushort>[] m_inverseStored = new Dictionary<NetOutgoingMessage, ushort>[NetConstants.NumReliableChannels];
		internal readonly NetBitVector m_storedMessagesNotEmpty = new NetBitVector(NetConstants.NumReliableChannels);

		private readonly ushort[] m_nextExpectedReliableSequence = new ushort[NetConstants.NumReliableChannels];
		private readonly List<NetIncomingMessage>[] m_withheldMessages = new List<NetIncomingMessage>[NetConstants.NetChannelsPerDeliveryMethod]; // only for ReliableOrdered
		internal readonly Queue<int> m_acknowledgesToSend = new Queue<int>();
		internal double m_nextForceAckTime;

		private readonly NetBitVector[] m_reliableReceived = new NetBitVector[NetConstants.NumSequenceNumbers];

		public int GetStoredMessagesCount()
		{
			int retval = 0;
			for (int i = 0; i < m_storedMessages.Length; i++)
			{
				var list = m_storedMessages[i];
				if (list != null)
					retval += list.Count;
			}
			return retval;
		}

		public int GetWithheldMessagesCount()
		{
			int retval = 0;
			for (int i = 0; i < m_withheldMessages.Length; i++)
			{
				var list = m_withheldMessages[i];
				if (list != null)
					retval += list.Count;
			}
			return retval;
		}

		private void InitializeReliability()
		{
			int num = ((int)NetMessageType.UserReliableOrdered + NetConstants.NetChannelsPerDeliveryMethod) - (int)NetMessageType.UserSequenced;
			m_nextSendSequenceNumber = new ushort[num];
			m_lastReceivedSequenced = new ushort[num];
			m_nextForceAckTime = double.MaxValue;
		}

		internal ushort GetSendSequenceNumber(NetMessageType mtp)
		{
			m_owner.VerifyNetworkThread();
			int slot = (int)mtp - (int)NetMessageType.UserSequenced;
			return m_nextSendSequenceNumber[slot]++;
		}

		internal static int Relate(int seqNr, int lastReceived)
		{
			return (seqNr < lastReceived ? (seqNr + NetConstants.NumSequenceNumbers) - lastReceived : seqNr - lastReceived);
		}

		// returns true if message should be rejected
		internal bool ReceivedSequencedMessage(NetMessageType mtp, ushort seqNr)
		{
			int slot = (int)mtp - (int)NetMessageType.UserSequenced;

			int diff = Relate(seqNr, m_lastReceivedSequenced[slot]);

			if (diff == 0)
				return true; // reject; already received
			if (diff > (ushort.MaxValue / 2))
				return true; // reject; out of window

			m_lastReceivedSequenced[slot] = seqNr;
			return false;
		}

		// called by Encode() to retrieve a sequence number and store the message for potential resending
		internal ushort StoreReliableMessage(double now, NetOutgoingMessage msg)
		{
			m_owner.VerifyNetworkThread();

			int seqNr = -1;

			int reliableSlot = (int)msg.m_type - (int)NetMessageType.UserReliableUnordered;
			Dictionary<ushort, NetOutgoingMessage> slotDict = m_storedMessages[reliableSlot];
			Dictionary<NetOutgoingMessage, ushort> invSlotDict = m_inverseStored[reliableSlot];
			if (slotDict == null)
			{
				slotDict = new Dictionary<ushort, NetOutgoingMessage>();
				m_storedMessages[reliableSlot] = slotDict;

				invSlotDict = new Dictionary<NetOutgoingMessage, ushort>();
				m_inverseStored[reliableSlot] = invSlotDict;

				// (cannot be a resend here)
			}
			else
			{
				// we assume there's a invSlotDict if there's a slotDict
				// is it a resend? if so, return the old sequence number
				ushort oldSeqNr;
				if (invSlotDict.TryGetValue(msg, out oldSeqNr))
					seqNr = oldSeqNr;
			}

			if (seqNr != -1)
			{
				// resend!
				// m_owner.LogDebug("Resending " + msg.m_type + "|" + seqNr);
				m_statistics.MessageResent();
			}
			else
			{
				// first send
				seqNr = GetSendSequenceNumber(msg.m_type);

				//m_owner.LogDebug("Sending " + msg.m_type + "|" + seqNr);

				Interlocked.Increment(ref msg.m_inQueueCount);
				slotDict.Add((ushort)seqNr, msg);
				invSlotDict.Add(msg, (ushort)seqNr);

				if (slotDict.Count > 0)
					m_storedMessagesNotEmpty.Set(reliableSlot, true);
			}

			// schedule next resend
			int numSends = msg.m_numSends;
			float[] baseTimes = m_peerConfiguration.m_resendBaseTime;
			float[] multiplers = m_peerConfiguration.m_resendRTTMultiplier;
			msg.m_nextResendTime = now + baseTimes[numSends] + (m_averageRoundtripTime * multiplers[numSends]);

			return (ushort)seqNr;
		}

		private void Resend(double now, ushort seqNr, NetOutgoingMessage msg)
		{
			m_owner.VerifyNetworkThread();

			int numSends = msg.m_numSends;
			float[] baseTimes = m_peerConfiguration.m_resendBaseTime;
			if (numSends >= baseTimes.Length)
			{
				// no more resends! We failed!
				int reliableSlot = (int)msg.m_type - (int)NetMessageType.UserReliableUnordered;
				m_storedMessages[reliableSlot].Remove(seqNr);
				m_owner.LogWarning("Failed to deliver reliable message " + msg + " (seqNr " + seqNr + ")");

				Disconnect("Failed to deliver reliable message!");

				return; // bye
			}

			m_owner.LogVerbose("Resending " + msg + " (seqNr " + seqNr + ")");

			Interlocked.Increment(ref msg.m_inQueueCount);
			m_unsentMessages.EnqueueFirst(msg);

			msg.m_lastSentTime = now;

			// schedule next resend
			float[] multiplers = m_peerConfiguration.m_resendRTTMultiplier;
			msg.m_nextResendTime = now + baseTimes[numSends] + (m_averageRoundtripTime * multiplers[numSends]);
		}

		private void HandleIncomingAcks(int ptr, int payloadByteLength)
		{
			m_owner.VerifyNetworkThread();

			int numAcks = payloadByteLength / 3;
			if (numAcks * 3 != payloadByteLength)
				m_owner.LogWarning("Malformed ack message; payload length is " + payloadByteLength);

			byte[] buffer = m_owner.m_receiveBuffer;
			for (int i = 0; i < numAcks; i++)
			{
				ushort seqNr = (ushort)(buffer[ptr++] | (buffer[ptr++] << 8));
				NetMessageType tp = (NetMessageType)buffer[ptr++];
				//m_owner.LogDebug("Got ack for " + tp + "|" + seqNr);

				// remove stored message
				int reliableSlot = (int)tp - (int)NetMessageType.UserReliableUnordered;

				var dict = m_storedMessages[reliableSlot];
				if (dict == null)
					continue;

				// find message
				NetOutgoingMessage om;
				if (dict.TryGetValue(seqNr, out om))
				{
					// found!
					dict.Remove(seqNr);
					m_inverseStored[reliableSlot].Remove(om);

					Interlocked.Decrement(ref om.m_inQueueCount);

					NetException.Assert(om.m_lastSentTime != 0);

					if (om.m_lastSentTime > m_lastSendRespondedTo)
						m_lastSendRespondedTo = om.m_lastSentTime;

					if (om.m_inQueueCount < 1)
						m_owner.Recycle(om);
				}

				// TODO: receipt handling
			}
		}

		private void ExpectedReliableSequenceArrived(int reliableSlot)
		{
			NetBitVector received = m_reliableReceived[reliableSlot];

			int nextExpected = m_nextExpectedReliableSequence[reliableSlot];

			if (received == null)
			{
				nextExpected = (nextExpected + 1) % NetConstants.NumSequenceNumbers;
				m_nextExpectedReliableSequence[reliableSlot] = (ushort)nextExpected;
				return;
			}

			received[(nextExpected + (NetConstants.NumSequenceNumbers / 2)) % NetConstants.NumSequenceNumbers] = false; // reset for next pass
			nextExpected = (nextExpected + 1) % NetConstants.NumSequenceNumbers;

			while (received[nextExpected] == true)
			{
				// it seems we've already received the next expected reliable sequence number

				// ordered?
				const int orderedSlotsStart = ((int)NetMessageType.UserReliableOrdered - (int)NetMessageType.UserReliableUnordered);
				if (reliableSlot >= orderedSlotsStart)
				{
					// ... then we should have a withheld message waiting

					// this should be a withheld message
					int orderedSlot = reliableSlot - orderedSlotsStart;
					bool foundWithheld = false;

					List<NetIncomingMessage> withheldList = m_withheldMessages[orderedSlot];
					if (withheldList != null)
					{
						foreach (NetIncomingMessage wm in withheldList)
						{
							int wmSeqChan = wm.SequenceChannel;

							if (orderedSlot == wmSeqChan && wm.m_sequenceNumber == nextExpected)
							{
								// Found withheld message due for delivery
								m_owner.LogVerbose("Releasing withheld message " + wm);

								// AcceptMessage
								m_owner.ReleaseMessage(wm);

								foundWithheld = true;
								withheldList.Remove(wm);

								// advance next expected
								received[(nextExpected + (NetConstants.NumSequenceNumbers / 2)) % NetConstants.NumSequenceNumbers] = false; // reset for next pass
								nextExpected = (nextExpected + 1) % NetConstants.NumSequenceNumbers;

								break;
							}
						}
					}
					if (!foundWithheld)
						throw new NetException("Failed to find withheld message!");
				}
			}

			m_nextExpectedReliableSequence[reliableSlot] = (ushort)nextExpected;
		}
	}
}
