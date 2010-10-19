using System;
using Lidgren.Network;

namespace UnitTests
{
	public static class BitVectorTests
	{
		public static void Run()
		{
			NetBitVector v = new NetBitVector(256);
			for (int i = 0; i < 256; i++)
			{
				v.Clear();
				if (i > 42 && i < 65)
					v = new NetBitVector(256);

				if (!v.IsEmpty())
					throw new NetException("bit vector fail 1");

				v.Set(i, true);

				if (v.Get(i) == false)
					throw new NetException("bit vector fail 2");

				if (v.IsEmpty())
					throw new NetException("bit vector fail 3");

				if (i != 79 && v.Get(79) == true)
					throw new NetException("bit vector fail 4");

				int f = v.GetFirstSetIndex();
				if (f != i)
					throw new NetException("bit vector fail 4");
			}

			/*
			v = new NetBitVector(9);
			v.Clear();
			v.Set(3, true);
			if (v.ToString() != "[000001000]")
				throw new NetException("NetBitVector.RotateDown failed");
			v.RotateDown();
			if (v.Get(3) == true || v.Get(2) == false || v.Get(4) == true)
				throw new NetException("NetBitVector.RotateDown failed 2");
			if (v.ToString() != "[000000100]")
				throw new NetException("NetBitVector.RotateDown failed 3");

			v.Set(0, true);
			v.RotateDown();
			if (v.ToString() != "[100000010]")
				throw new NetException("NetBitVector.RotateDown failed 4");

			v = new NetBitVector(38);
			v.Set(0, true);
			v.Set(1, true);
			v.Set(31, true);

			if (v.ToString() != "[00000010000000000000000000000000000011]")
				throw new NetException("NetBitVector.RotateDown failed 5");

			v.RotateDown();

			if (v.ToString() != "[10000001000000000000000000000000000001]")
				throw new NetException("NetBitVector.RotateDown failed 5");
			*/

			Console.WriteLine("NetBitVector tests OK");
		}
	}
}
