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
		IBitfield AckBitfield { get; set; }
		PacketType Type { get; set; }
		byte[] Data { get; set; }
		uint Crc32 { get; set; }

		byte[] ToBytes();
		void FromBytes(byte[] buffer);
		void FromBytes(byte[] buffer, int offset, int length);
	}
}
