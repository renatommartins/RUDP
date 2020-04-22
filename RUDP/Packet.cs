using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Enumerations;
using RUDP.Interfaces;

namespace RUDP
{
	public class Packet : IPacket
	{
		public ushort AppId { get; set; }
		public ushort SequenceNumber { get; set; }
		public ushort AckSequenceNumber { get; set; }

		public IBitfield AckBitfield { get; set; }
		public PacketType Type { get; set; }
		public byte[] Data { get; set; }
		public uint Crc32 { get; set; }
	}
}
