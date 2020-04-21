using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Enumerations;

namespace RUDP.Interfaces
{
	public interface IPacket
	{
		ushort AppId { get; set; }
		ushort SequenceNumber { get; set; }
		ushort AckSequenceNumber { get; set; }
		IBitfield Bitfield { get; set; }
		PacketType Type { get; set; }
		byte[] Data { get; set; }
		uint Crc32 { get; set; }
	}
}
