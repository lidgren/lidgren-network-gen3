using System;
using System.Collections.Generic;

using Lidgren.Network;
using System.Reflection;
using System.Text;

namespace UnitTests
{
	public static class ReadWriteTests
	{
		public static void Run(NetPeer peer)
		{
			NetOutgoingMessage msg = peer.CreateMessage();

			msg.Write(false);
			msg.Write(-3, 6);
			msg.Write(42);
			msg.Write("duke of earl");
			msg.Write((byte)43);
			msg.Write((ushort)44);
			msg.Write(true);

			msg.WritePadBits();
			
			msg.Write(45.0f);
			msg.Write(46.0);
			msg.WriteVariableInt32(-47);
			msg.WriteVariableUInt32(48);

			byte[] data = msg.PeekDataBuffer();

			NetIncomingMessage inc = Program.CreateIncomingMessage(data, msg.LengthBits);

			StringBuilder bdr = new StringBuilder();

			bdr.Append(inc.ReadBoolean());
			bdr.Append(inc.ReadInt32(6));
			bdr.Append(inc.ReadInt32());
			bdr.Append(inc.ReadString());
			bdr.Append(inc.ReadByte());

			if (inc.PeekUInt16() != (ushort)44)
				throw new NetException("Read/write failure");

			bdr.Append(inc.ReadUInt16());
			bdr.Append(inc.ReadBoolean());
			
			inc.SkipPadBits();

			bdr.Append(inc.ReadSingle());
			bdr.Append(inc.ReadDouble());
			bdr.Append(inc.ReadVariableInt32());
			bdr.Append(inc.ReadVariableUInt32());

			if (bdr.ToString().Equals("False-342duke of earl4344True4546-4748"))
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

			data = tmp.PeekDataBuffer();

			inc = Program.CreateIncomingMessage(data, tmp.LengthBits);

			Test readTest = new Test();
			inc.ReadAllFields(readTest, BindingFlags.Public | BindingFlags.Instance);

			NetException.Assert(readTest.Number == 42);
			NetException.Assert(readTest.Name == "Hallon");
			NetException.Assert(readTest.Age == 8.2f);

			msg = peer.CreateMessage();

			System.IO.BinaryWriter br = new System.IO.BinaryWriter(msg);

			br.Write(true);
			br.Write("hallon");
			br.Write((byte)42);

			int byteLen = msg.LengthBytes;
			byte[] rbts = msg.PeekDataBuffer();

			inc = Program.CreateIncomingMessage(rbts, msg.LengthBits);

			System.IO.BinaryReader rdr = new System.IO.BinaryReader(inc);

			bool one = rdr.ReadBoolean();
			string hallon = rdr.ReadString();
			byte fourtyTwo = rdr.ReadByte();

			// test aligned WriteBytes/ReadBytes
			msg = peer.CreateMessage();
			byte[] tmparr = new byte[] { 5, 6, 7, 8, 9 };
			msg.Write(tmparr);

			inc = Program.CreateIncomingMessage(msg.PeekDataBuffer(), msg.LengthBits);
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
