using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace UnitTests
{
	public static class NetQueueTests
	{
		public static void Run()
		{
			NetQueue<int> queue = new NetQueue<int>(8);

			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);

			bool ok;
			int a;

			if (queue.Contains(4))
				throw new Exception("NetQueue Contains failure");

			if (!queue.Contains(2))
				throw new Exception("NetQueue Contains failure 2");

			if (queue.Count != 3)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == false || a != 1)
				throw new Exception("NetQueue failure");

			if (queue.Count != 2)
				throw new Exception("NetQueue failed");

			queue.EnqueueFirst(42);
			if (queue.Count != 3)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == false || a != 42)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == false || a != 2)
				throw new Exception("NetQueue failed");
	
			ok = queue.TryDequeue(out a);
			if (ok == false || a != 3)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == true)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == true)
				throw new Exception("NetQueue failed");

			queue.Enqueue(78);
			if (queue.Count != 1)
				throw new Exception("NetQueue failed");

			ok = queue.TryDequeue(out a);
			if (ok == false || a != 78)
				throw new Exception("NetQueue failed");

			queue.Clear();
			if (queue.Count != 0)
				throw new Exception("NetQueue.Clear failed");

			Console.WriteLine("NetQueue tests OK");
		}
	}
}
