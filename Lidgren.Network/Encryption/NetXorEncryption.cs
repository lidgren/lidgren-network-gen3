using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	/// <summary>
	/// Example class; not very good encryption
	/// </summary>
	public class NetXorEncryption : INetEncryption
	{
		private byte[] m_key;

		public NetXorEncryption(byte[] key)
		{
			m_key = key;
		}

		public NetXorEncryption(string key)
		{
			m_key = Encoding.ASCII.GetBytes(key);
		}

		public bool Encrypt(NetOutgoingMessage msg)
		{
			int numBytes = msg.LengthBytes;
			for (int i = 0; i < numBytes; i++)
			{
				int offset = i % m_key.Length;
				msg.m_data[i] = (byte)(msg.m_data[i] ^ m_key[offset]);
			}
			return true;
		}

		public bool Decrypt(NetIncomingMessage msg)
		{
			int numBytes = msg.LengthBytes;
			for (int i = 0; i < numBytes; i++)
			{
				int offset = i % m_key.Length;
				msg.m_data[i] = (byte)(msg.m_data[i] ^ m_key[offset]);
			}
			return true;
		}
	}
}
