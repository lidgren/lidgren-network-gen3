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

namespace Lidgren.Network
{
	public sealed class NetBitVector
	{
		private readonly int m_capacity;
		private readonly uint[] m_data;

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

		[System.Runtime.CompilerServices.IndexerName("Bit")]
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
