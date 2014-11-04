using System;
using System.Collections.Generic;

using Lidgren.Network;
using System.Threading;
using System.Net;

namespace Client
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration config = new NetPeerConfiguration("garbagethrower");
			var client = new NetClient(config);

			client.Start();

			var target = new IPEndPoint(NetUtility.Resolve("localhost"), 14242);
			var buffer = new byte[1024];
			var rnd = new Random();

			int batch = 0;
			// use RawSend to throw poop at server
			while(true)
			{
				rnd.NextBytes(buffer);
				int length = rnd.Next(1, 1023);

				switch (rnd.Next(2))
				{
					case 0:
						// complete randomness
						break;
					case 1:
						// semi-sensical
						buffer[1] = 0; // not a fragment, sequence number 0
						buffer[2] = 0; // not a fragment, sequence number 0
						buffer[3] = (byte)length;			// correct payload length
						buffer[4] = (byte)(length >> 8);	// correct payload length
						break;
				}

				// fling teh poop
				client.RawSend(buffer, 0, length, target);

				batch++;
				if (batch >= 3)
				{
					batch = 0;
					Thread.Sleep(0);
				}
			}
		}
	}
}
