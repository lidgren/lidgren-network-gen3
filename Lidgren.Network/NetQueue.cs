using System;
using System.Diagnostics;

namespace Lidgren.Network
{
	/// <summary>
	/// Thread safe queue with TryDequeue()
	/// </summary>
	[DebuggerDisplay("Count={m_size}")]
	public sealed class NetQueue<T>
	{
		// Example:
		// m_capacity = 8
		// m_size = 6
		// m_head = 4
		//
		// [0] item
		// [1] item (tail = ((head + size - 1) % capacity)
		// [2] 
		// [3] 
		// [4] item (head)
		// [5] item
		// [6] item 
		// [7] item
		//
		private T[] m_items;
		private object m_lock;
		private int m_size;
		private int m_head;

		public int Count { get { return m_size; } }

		public NetQueue(int initialCapacity)
		{
			m_lock = new object();
			m_items = new T[initialCapacity];
		}

		/// <summary>
		/// Places an item last/tail of the queue
		/// </summary>
		public void Enqueue(T item)
		{
#if DEBUG
			if (typeof(T) == typeof(NetOutgoingMessage))
			{
				NetOutgoingMessage om = item as NetOutgoingMessage;
				if (om != null)
					if (om.m_type == NetMessageType.Error)
						throw new NetException("Enqueuing error message!");
			}
#endif
			lock (m_lock)
			{
				if (m_size == m_items.Length)
					SetCapacity(m_items.Length + 8);
							
				int slot = (m_head + m_size) % m_items.Length;
				m_items[slot] = item;
				m_size++;
			}
		}

		/// <summary>
		/// Places an item first, at the head of the queue
		/// </summary>
		public void EnqueueFirst(T item)
		{
			lock (m_lock)
			{
				if (m_size >= m_items.Length)
					SetCapacity(m_items.Length + 8);
			
				m_head--;
				if (m_head < 0)
					m_head = m_items.Length - 1;
				m_items[m_head] = item;
				m_size++;
			}
		}

		private void SetCapacity(int newCapacity)
		{
			if (m_size == 0)
			{
				lock (m_lock)
				{
					if (m_size == 0)
					{
						m_items = new T[newCapacity];
						m_head = 0;
						return;
					}
				}
			}

			T[] newItems = new T[newCapacity];

			lock (m_lock)
			{
				if (m_head + m_size - 1 < m_items.Length)
				{
					Array.Copy(m_items, m_head, newItems, 0, m_size);
				}
				else
				{
					Array.Copy(m_items, m_head, newItems, 0, m_items.Length - m_head);
					Array.Copy(m_items, 0, newItems, m_items.Length - m_head, (m_size - (m_items.Length - m_head)));
				}

				m_items = newItems;
				m_head = 0;
			}
		}

		/// <summary>
		/// Gets an item from the head of the queue, or returns default(T) if empty
		/// </summary>
		public T TryDequeue()
		{
			if (m_size == 0)
				return default(T);

			lock (m_lock)
			{
				if (m_size == 0)
					return default(T);

				T retval = m_items[m_head];
				m_items[m_head] = default(T);

				m_head = (m_head + 1) % m_items.Length;
				m_size--;

				return retval;
			}
		}

		public bool Contains(T item)
		{
			lock (m_lock)
			{
				int ptr = m_head;
				for (int i = 0; i < m_size; i++)
				{
					if (m_items[ptr].Equals(item))
						return true;
					ptr = (ptr + 1) % m_items.Length;
				}
			}
			return false;
		}

		public void Clear()
		{
			lock (m_lock)
			{
				for (int i = 0; i < m_items.Length; i++)
					m_items[i] = default(T);
				m_head = 0;
			}
		}
	}
}
