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
	public partial class NetPeer
	{
		internal int m_storedBytes;
		private int m_maxStoredBytes;
		private List<byte[]> m_storagePool = new List<byte[]>();
		private NetQueue<NetIncomingMessage> m_incomingMessagesPool = new NetQueue<NetIncomingMessage>(16);
		private NetQueue<NetOutgoingMessage> m_outgoingMessagesPool = new NetQueue<NetOutgoingMessage>(16);

		private void InitializeRecycling()
		{
			m_storagePool.Clear();
			m_storedBytes = 0;
			m_maxStoredBytes = m_configuration.m_maxRecycledBytesKept;
			m_incomingMessagesPool.Clear();
			m_outgoingMessagesPool.Clear();
		}
		
		internal byte[] GetStorage(int requiredBytes)
		{
			if (m_storagePool.Count < 1)
			{
				m_statistics.m_bytesAllocated += requiredBytes;
				return new byte[requiredBytes];
			}

			lock (m_storagePool)
			{
				// search from end to start
				for (int i = m_storagePool.Count - 1; i >= 0; i--)
				{
					byte[] retval = m_storagePool[i];
					if (retval.Length >= requiredBytes)
					{
						m_storagePool.RemoveAt(i);
						m_storedBytes -= retval.Length;

						return retval;
					}
				}
			}

			m_statistics.m_bytesAllocated += requiredBytes;
			return new byte[requiredBytes];
		}

		/// <summary>
		/// Creates a new message for sending
		/// </summary>
		public NetOutgoingMessage CreateMessage()
		{
			return CreateMessage(m_configuration.DefaultOutgoingMessageCapacity);
		}

		/// <summary>
		/// Creates a new message for sending
		/// </summary>
		/// <param name="initialCapacity">initial capacity in bytes</param>
		public NetOutgoingMessage CreateMessage(int initialCapacity)
		{
			NetOutgoingMessage retval = m_outgoingMessagesPool.TryDequeue();
			if (retval == null)
				retval = new NetOutgoingMessage();
			else
				retval.Reset();

			byte[] storage = GetStorage(initialCapacity);
			retval.m_data = storage;

			return retval;
		}

		internal NetOutgoingMessage CreateLibraryMessage(NetMessageLibraryType tp, string content)
		{
			NetOutgoingMessage retval = CreateMessage(1 + (content == null ? 0 : content.Length));
			retval.m_type = NetMessageType.Library;
			retval.m_libType = tp;
			retval.Write((content == null ? "" : content));
			return retval;
		}

		/// <summary>
		/// Recycle the message to the library for reuse
		/// </summary>
		public void Recycle(NetIncomingMessage msg)
		{
			if (msg.m_status != NetIncomingMessageReleaseStatus.ReleasedToApplication)
				throw new NetException("Message not under application control; recycled more than once?");

			msg.m_status = NetIncomingMessageReleaseStatus.RecycledByApplication;
			if (msg.m_data != null)
			{
				lock (m_storagePool)
				{
#if DEBUG
					if (m_storagePool.Contains(msg.m_data))
						throw new NetException("Storage pool object recycled twice!");
#endif
					m_storedBytes += msg.m_data.Length;
					m_storagePool.Add(msg.m_data);
				}
				msg.m_data = null;
			}
			m_incomingMessagesPool.Enqueue(msg);
		}

		/// <summary>
		/// Recycle the message to the library for reuse
		/// </summary>
		internal void Recycle(NetOutgoingMessage msg)
		{
			VerifyNetworkThread();

#if DEBUG
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
				{
					if (conn.m_unsentMessages.Contains(msg))
						throw new NetException("Ouch! Recycling unsent message!");

					for(int i=0;i<conn.m_storedMessages.Length;i++)
					{
						List<NetOutgoingMessage> list = conn.m_storedMessages[i];
						if (list != null && list.Count > 0)
						{
							foreach (NetOutgoingMessage om in conn.m_storedMessages[i])
							{
								if (om == msg)
									throw new NetException("Ouch! Recycling stored message!");
							}
						}
					}
				}
			}
#endif
			NetException.Assert(msg.m_inQueueCount == 0, "Recycling message still in some queue!");

			if (msg.m_data != null)
			{
				lock (m_storagePool)
				{
					if (!m_storagePool.Contains(msg.m_data))
					{
						m_storedBytes += msg.m_data.Length;
						m_storagePool.Add(msg.m_data);
					}
				}
				msg.m_data = null;
			}
			m_outgoingMessagesPool.Enqueue(msg);
		}

		/// <summary>
		/// Call to check if storage pool should be reduced
		/// </summary>
		private void ReduceStoragePool()
		{
			VerifyNetworkThread();

			if (m_storedBytes < m_configuration.m_maxRecycledBytesKept)
				return; // never mind threading, no big deal if storage is larger than config setting for a frame

			int wasStoredBytes;
			int reduceTo;
			lock (m_storagePool)
			{
				// since newly stored message at added to the end; remove from the start
				wasStoredBytes = m_storedBytes;
				reduceTo = m_maxStoredBytes / 2;

				while (m_storedBytes > reduceTo && m_storagePool.Count > 0)
				{
					byte[] arr = m_storagePool[0];
					m_storedBytes -= arr.Length;
					m_storagePool.RemoveAt(0);
				}
			}

			// done
			LogDebug("Reduced recycled bytes pool from " + wasStoredBytes + " bytes to " + m_storedBytes + " bytes (target " + reduceTo + ")");

			return;
		}

		/// <summary>
		/// Creates an incoming message with the required capacity for releasing to the application
		/// </summary>
		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, string contents)
		{
			NetIncomingMessage retval;
			if (string.IsNullOrEmpty(contents))
			{
				retval = CreateIncomingMessage(tp, 1);
				retval.Write("");
				return retval;
			}

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(contents);
			retval = CreateIncomingMessage(tp, bytes.Length + (bytes.Length > 127 ? 2 : 1));
			retval.Write(contents);

			return retval;
		}

		/// <summary>
		/// Creates an incoming message with the required capacity for releasing to the application
		/// </summary>
		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, int requiredCapacity)
		{
			NetIncomingMessage retval = m_incomingMessagesPool.TryDequeue();
			if (retval == null)
				retval = new NetIncomingMessage();
			else
				retval.Reset();

			NetException.Assert(retval.m_status != NetIncomingMessageReleaseStatus.ReleasedToApplication);

			retval.m_incomingType = tp;
			retval.m_senderConnection = null;
			retval.m_senderEndpoint = null;
			retval.m_status = NetIncomingMessageReleaseStatus.NotReleased;

			if (requiredCapacity > 0)
			{
				byte[] storage = GetStorage(requiredCapacity);
				retval.m_data = storage;
			}
			else
			{
				retval.m_data = null;
			}

			return retval;
		}

		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, byte[] copyFrom, int offset, int copyLength)
		{
			NetIncomingMessage retval = m_incomingMessagesPool.TryDequeue();
			if (retval == null)
				retval = new NetIncomingMessage();
			else
				retval.Reset();

			retval.m_data = GetStorage(copyLength);
			Buffer.BlockCopy(copyFrom, offset, retval.m_data, 0, copyLength);

			retval.m_bitLength = copyLength * 8;
			retval.m_incomingType = tp;
			retval.m_senderConnection = null;
			retval.m_senderEndpoint = null;

			return retval;
		}

	}
}
