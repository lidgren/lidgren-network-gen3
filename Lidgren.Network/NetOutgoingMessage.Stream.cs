using System;
using System.IO;

namespace Lidgren.Network
{
	public partial class NetOutgoingMessage : Stream
	{
		public override bool CanRead { get { return false; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }

		public override void Flush()
		{
			// no op
		}

		/// <summary>
		/// Gets the length of the stream, in bytes
		/// </summary>
		public override long Length
		{
			get { return (long)LengthBytes; }
		}

		public override long Position
		{
			get
			{
				throw new NetException("Position in bytes is not relevant since the bit count can vary");
			}
			set
			{
				throw new NetException("It's not possible to seek in this message");
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NetException("It's not possible to read from this message");
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NetException("It's not possible to seek in this message");
		}

		public override void SetLength(long value)
		{
			throw new NetException("It's not possible to set the length of this message");
		}
	}
}
