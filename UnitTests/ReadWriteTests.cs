using System;
using System.Collections.Generic;

using Lidgren.Network;
using System.Reflection;
using System.Text;

namespace UnitTests
{
	public static class ReadWriteTests
	{
		public static string ToBinaryString(ulong value, int bits, bool includeSpaces)
		{
			int numSpaces = Math.Max(0, (bits / 8) - 1);
			if (includeSpaces == false)
				numSpaces = 0;

			StringBuilder bdr = new StringBuilder(bits + numSpaces);
			for (int i = 0; i < bits + numSpaces; i++)
				bdr.Append(' ');

			for (int i = 0; i < bits; i++)
			{
				ulong shifted = (ulong)(value >> i);
				bool isSet = ((shifted & 1) != 0);

				int pos = bits - 1 - i;
				if (includeSpaces)
					pos += Math.Max(0, (pos / 8));

				bdr[pos] = (isSet ? '1' : '0');
			}
			return bdr.ToString();
		}

		public static void Run(NetPeer peer)
		{
			NetOutgoingMessage msg = peer.CreateMessage();

			msg.Write(false);
			msg.Write(-3, 6);
			msg.Write(42);
			msg.Write("duke of earl");
			msg.Write((byte)43);
			msg.Write((ushort)44);
			msg.Write(UInt64.MaxValue, 64);
			msg.Write(true);

			msg.WritePadBits();

			int bcnt = 0;

			msg.Write(567845.0f);
			msg.WriteVariableInt32(2115998022);
			msg.Write(46.0);
			msg.Write((ushort)14, 9);
			bcnt += msg.WriteVariableInt32(-47);
			msg.WriteVariableInt32(470000);
			msg.WriteVariableUInt32(48);
			bcnt += msg.WriteVariableInt64(-49);

			if (bcnt != 2)
				throw new NetException("WriteVariable* wrote too many bytes!");

			byte[] data = msg.Data;

			NetIncomingMessage inc = Program.CreateIncomingMessage(data, msg.LengthBits);

			StringBuilder bdr = new StringBuilder();

			bdr.Append(inc.ReadBoolean());
			bdr.Append(inc.ReadInt32(6));
			bdr.Append(inc.ReadInt32());

			string strResult;
			bool ok = inc.ReadString(out strResult);
			if (ok == false)
				throw new NetException("Read/write failure");
			bdr.Append(strResult);
			
			bdr.Append(inc.ReadByte());

			if (inc.PeekUInt16() != (ushort)44)
				throw new NetException("Read/write failure");

			bdr.Append(inc.ReadUInt16());

			if (inc.PeekUInt64(64) != UInt64.MaxValue)
				throw new NetException("Read/write failure");

			bdr.Append(inc.ReadUInt64());
			bdr.Append(inc.ReadBoolean());
		
			inc.SkipPadBits();

			bdr.Append(inc.ReadSingle());
			bdr.Append(inc.ReadVariableInt32());
			bdr.Append(inc.ReadDouble());
			bdr.Append(inc.ReadUInt32(9));
			bdr.Append(inc.ReadVariableInt32());
			bdr.Append(inc.ReadVariableInt32());
			bdr.Append(inc.ReadVariableUInt32());
			bdr.Append(inc.ReadVariableInt64());

			if (bdr.ToString().Equals("False-342duke of earl434418446744073709551615True56784521159980224614-4747000048-49"))
				Console.WriteLine("Read/write tests OK");
			else
				throw new NetException("Read/write tests FAILED!");

			msg = peer.CreateMessage();

			NetOutgoingMessage tmp = peer.CreateMessage();
			tmp.Write((int)42, 14);

			msg.Write(tmp);
			msg.Write(tmp);

			if (msg.LengthBits != tmp.LengthBits * 2)
				throw new NetException("NetOutgoingMessage.Write(NetOutgoingMessage) failed!");

			tmp = peer.CreateMessage();

			Test test = new Test();
			test.Number = 42;
			test.Name = "Hallon";
			test.Age = 8.2f;

			tmp.WriteAllFields(test, BindingFlags.Public | BindingFlags.Instance);

			data = tmp.Data;

			inc = Program.CreateIncomingMessage(data, tmp.LengthBits);

			Test readTest = new Test();
			inc.ReadAllFields(readTest, BindingFlags.Public | BindingFlags.Instance);

			NetException.Assert(readTest.Number == 42);
			NetException.Assert(readTest.Name == "Hallon");
			NetException.Assert(readTest.Age == 8.2f);
			
			// test aligned WriteBytes/ReadBytes
			msg = peer.CreateMessage();
			byte[] tmparr = new byte[] { 5, 6, 7, 8, 9 };
			msg.Write(tmparr);

			inc = Program.CreateIncomingMessage(msg.Data, msg.LengthBits);
			byte[] result = inc.ReadBytes(tmparr.Length);

			for (int i = 0; i < tmparr.Length; i++)
				if (tmparr[i] != result[i])
					throw new Exception("readbytes fail");
		}
	}

	public class TestBase
	{
		public int Number;
	}

	public class Test : TestBase
	{
		public float Age;
		public string Name;
	}
}
