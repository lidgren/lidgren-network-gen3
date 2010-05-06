using System;

namespace Lidgren.Network
{
	public sealed class NetBitVector
	{
		private int m_capacity;
		private uint[] m_data;

		public int Capacity { get { return m_capacity; } }

		public NetBitVector(int bitsCapacity)
		{
			m_capacity = bitsCapacity;
			m_data = new uint[(bitsCapacity + 31) / 32];
		}

		public bool IsEmpty()
		{
			foreach (uint v in m_data)
				if (v != 0)
					return false;
			return true;
		}

		public int GetFirstSetIndex()
		{
			int idx = 0;

			uint data = m_data[0];
			while (data == 0)
			{
				idx++;
				data = m_data[idx];
			}

			int a = 0;
			while (((data >> a) & 1) == 0)
				a++;

			return (idx * 32) + a;
		}

		public bool Get(int bitIndex)
		{
			int idx = bitIndex / 32;
			uint data = m_data[idx];
			int bitNr = bitIndex - (idx * 32);
			return (data & (1 << bitNr)) != 0;
		}

		public void Set(int bitIndex, bool value)
		{
			int idx = bitIndex / 32;
			int bitNr = bitIndex - (idx * 32);
			if (value)
				m_data[idx] |= (uint)(1 << bitNr);
			else
				m_data[idx] &= (uint)(~(1 << bitNr));
		}

		public bool this [int index]
		{
			get { return Get(index); }
			set { Set(index, value); }
		}

		public void Clear()
		{
			Array.Clear(m_data, 0, m_data.Length);
		}
	}
}
