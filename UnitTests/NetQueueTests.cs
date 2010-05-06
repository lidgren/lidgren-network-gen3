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

			if (queue.Count != 3)
				throw new Exception("NetQueue failed");
			if (queue.TryDequeue() != 1)
				throw new Exception("NetQueue failure");
			if (queue.Count != 2)
				throw new Exception("NetQueue failed");

			queue.EnqueueFirst(42);
			if (queue.Count != 3)
				throw new Exception("NetQueue failed");

			if (queue.TryDequeue() != 42)
				throw new Exception("NetQueue failed");
			if (queue.TryDequeue() != 2)
				throw new Exception("NetQueue failed");
			if (queue.TryDequeue() != 3)
				throw new Exception("NetQueue failed");
			if (queue.TryDequeue() != 0)
				throw new Exception("NetQueue failed");
			if (queue.TryDequeue() != 0)
				throw new Exception("NetQueue failed");

			queue.Enqueue(78);
			if (queue.Count != 1)
				throw new Exception("NetQueue failed");

			if (queue.TryDequeue() != 78)
				throw new Exception("NetQueue failed");

			queue.Clear();
			if (queue.Count != 0)
				throw new Exception("NetQueue.Clear failed");

			Console.WriteLine("NetQueue tests OK");
		}
	}
}
