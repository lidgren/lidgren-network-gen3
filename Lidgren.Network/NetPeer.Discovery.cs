using System;
using System.Net;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		/// <summary>
		/// Emit a discovery signal to all hosts on your subnet
		/// </summary>
		public void DiscoverLocalPeers(int serverPort)
		{
			NetOutgoingMessage om = CreateMessage();
			SendUnconnectedLibraryMessage(om, NetMessageLibraryType.Discovery, new IPEndPoint(IPAddress.Broadcast, serverPort));
		}

		/// <summary>
		/// Emit a discovery signal to a single known host
		/// </summary>
		public bool DiscoverKnownPeer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			if (address == null)
				return false;
			return DiscoverKnownPeer(new IPEndPoint(address, serverPort));
		}

		/// <summary>
		/// Emit a discovery signal to a single known host
		/// </summary>
		public bool DiscoverKnownPeer(IPEndPoint endpoint)
		{
			NetOutgoingMessage om = CreateMessage();
			SendUnconnectedLibraryMessage(om, NetMessageLibraryType.Discovery, endpoint);
			return true;
		}
	}
}
