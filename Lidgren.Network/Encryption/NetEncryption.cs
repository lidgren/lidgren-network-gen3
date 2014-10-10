using System;
using System.Collections.Generic;

namespace Lidgren.Network
{
	/// <summary>
	/// Interface for an encryption algorithm
	/// </summary>
	public abstract class NetEncryption
	{
		/// <summary>
		/// NetPeer
		/// </summary>
		protected NetPeer m_peer;

		/// <summary>
		/// Constructor
		/// </summary>
		public NetEncryption(NetPeer peer)
		{
			if (peer == null)
				throw new NetException("Peer must not be null");
			m_peer = peer;
		}

		/// <summary>
		/// Encrypt an outgoing message in place
		/// </summary>
		public abstract bool Encrypt(NetOutgoingMessage msg);

		/// <summary>
		/// Decrypt an incoming message in place
		/// </summary>
		public abstract bool Decrypt(NetIncomingMessage msg);
	}
}
