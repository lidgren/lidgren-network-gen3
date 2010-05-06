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

		internal List<NetOutgoingMessage>[] m_storedMessages; // naïve! replace by something better?
		internal NetBitVector m_storedMessagesNotEmpty;

		private ushort[] m_nextExpectedReliableSequence;
		private List<NetIncomingMessage>[] m_withheldMessages;
		internal Queue<int> m_acknowledgesToSend;
		internal double m_nextForceAckTime;

		private NetBitVector[] m_reliableReceived;

		public int GetStoredMessagesCount()
		{
			int retval = 0;
			for (int i = 0; i < m_storedMessages.Length; i++)
			{
				List<NetOutgoingMessage> list = m_storedMessages[i];
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

			m_storedMessages = new List<NetOutgoingMessage>[NetConstants.NumReliableChannels];
			m_storedMessagesNotEmpty = new NetBitVector(NetConstants.NumReliableChannels);

			m_reliableReceived = new NetBitVector[NetConstants.NumSequenceNumbers];
			m_nextExpectedReliableSequence = new ushort[NetConstants.NumReliableChannels];
			m_withheldMessages = new List<NetIncomingMessage>[NetConstants.NetChannelsPerDeliveryMethod]; // only for ReliableOrdered
			m_acknowledgesToSend = new Queue<int>();
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

		// called the FIRST time a reliable message is sent
		private void StoreReliableMessage(double now, NetOutgoingMessage msg)
		{
			m_owner.VerifyNetworkThread();

			int reliableSlot = (int)msg.m_type - (int)NetMessageType.UserReliableUnordered;

			List<NetOutgoingMessage> list = m_storedMessages[reliableSlot];
			if (list == null)
			{
				list = new List<NetOutgoingMessage>();
				m_storedMessages[reliableSlot] = list;
			}
			Interlocked.Increment(ref msg.m_inQueueCount);
			list.Add(msg);

			if (list.Count == 1)
				m_storedMessagesNotEmpty.Set(reliableSlot, true);

			// schedule next resend
			int numSends = msg.m_numSends;
			float[] baseTimes = m_peerConfiguration.m_resendBaseTime;
			float[] multiplers = m_peerConfiguration.m_resendRTTMultiplier;
			msg.m_nextResendTime = now + baseTimes[numSends] + (m_averageRoundtripTime * multiplers[numSends]);
		}

		private void Resend(double now, NetOutgoingMessage msg)
		{
			m_owner.VerifyNetworkThread();

			int numSends = msg.m_numSends;
			float[] baseTimes = m_peerConfiguration.m_resendBaseTime;
			if (numSends >= baseTimes.Length)
			{
				// no more resends! We failed!
				int reliableSlot = (int)msg.m_type - (int)NetMessageType.UserReliableUnordered;
				List<NetOutgoingMessage> list = m_storedMessages[reliableSlot];
				list.Remove(msg);
				m_owner.LogWarning("Failed to deliver reliable message " + msg);

				Disconnect("Failed to deliver reliable message!");

				return; // bye
			}

			m_owner.LogVerbose("Resending " + msg);

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
				// m_owner.LogDebug("Got ack for " + tp + " " + seqNr);

				// remove stored message
				int reliableSlot = (int)tp - (int)NetMessageType.UserReliableUnordered;

				List<NetOutgoingMessage> list = m_storedMessages[reliableSlot];
				if (list == null)
					continue;

				// find message
				for (int a = 0; a < list.Count; a++)
				{
					NetOutgoingMessage om = list[a];
					if (om.m_sequenceNumber == seqNr)
					{
						// found!
						list.RemoveAt(a);
						Interlocked.Decrement(ref om.m_inQueueCount);

						NetException.Assert(om.m_lastSentTime != 0);

						if (om.m_lastSentTime > m_lastSendRespondedTo)
							m_lastSendRespondedTo = om.m_lastSentTime;

						if (om.m_inQueueCount < 1)
							m_owner.Recycle(om);

						break;
					}
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
