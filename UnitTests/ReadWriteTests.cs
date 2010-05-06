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

			NetIncomingMessage inc = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
			typeof(NetIncomingMessage).GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, data);
			typeof(NetIncomingMessage).GetField("m_bitLength", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, msg.LengthBits);

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
		}
	}
}
