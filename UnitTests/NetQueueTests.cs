using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace UnitTests
{
	public static class NetQueueTests
	{
		public static void Run()
		{
			NetQueue<int> queue = new NetQueue<int>(4);

			queue.Enqueue(1);
			queue.Enqueue(2);
			queue.Enqueue(3);

			int[] arr = queue.ToArray();
			if (arr.Length != 3)
				throw new Exception("NetQueue.ToArray failure");
			if (arr[0] != 1 || arr[1] != 2 || arr[2] != 3)
				throw new Exception("NetQueue.ToArray failure");

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

			int[] arr2 = queue.ToArray();
			if (arr2.Length != 0)
				throw new Exception("NetQueue.ToArray failure");

			Console.WriteLine("NetQueue tests OK");
		}
	}
}
