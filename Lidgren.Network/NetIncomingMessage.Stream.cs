using System;
using System.IO;

namespace Lidgren.Network
{
	public partial class NetIncomingMessage : Stream
	{
		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return false; } }

		public override void Flush()
		{
			// no op
		}

		/// <summary>
		/// Gets the length in bytes
		/// </summary>
		public override long Length
		{
			get { return LengthBytes; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			// limit amount to remaining
			int remainingBytes = NetUtility.BytesToHoldBits(m_bitLength - m_readPosition);
			if (count > remainingBytes)
				count = remainingBytes;
			if (count < 1)
				return 0;

			ReadBytes(buffer, offset, count);
			return count;
		}

		/// <summary>
		/// Sets the position in the stream, in bytes
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					Position = (offset * 8);
					break;
				case SeekOrigin.Current:
					Position = Position + (offset * 8);
					break;
				case SeekOrigin.End:
					Position = (LengthBytes - offset) * 8;
					break;
				default:
					throw new NotImplementedException("Bad SeekOrigin");
			}
			return Position;
		}

		public override void SetLength(long value)
		{
			throw new NetException("It's not possible to set the length of the NetIncomingMessage");
		}
	}
}
