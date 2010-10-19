using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidgren.Network
{
	/// <summary>
	/// How the library deals with resends and handling of late messages
	/// </summary>
	public enum NetDeliveryMethod : byte
	{
		//
		// Actually a publicly visible subset of NetMessageType
		//
		Unknown = 0,
		Unreliable = 1,
		UnreliableSequenced = 2,
		ReliableUnordered = 34,
		ReliableSequenced = 35,
		ReliableOrdered = 67,
	}
}
