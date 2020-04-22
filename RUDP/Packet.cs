using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Enumerations;
using RUDP.Interfaces;

namespace RUDP
{
	public class Packet : IPacket
	{
		private const int _appIdOffset = 0;
		public ushort AppId { get; set; }

		private const int _seqNumOffset = _appIdOffset + sizeof(ushort);
		public ushort SequenceNumber { get; set; }

		private const int _ackSeqNumOffset = _seqNumOffset + sizeof(ushort);
		public ushort AckSequenceNumber { get; set; }

		private const int _ackBitfieldOffset = _ackSeqNumOffset + sizeof(ushort);
		public IBitfield AckBitfield { get; set; }

		private const int _typeOffset = _ackBitfieldOffset + sizeof(byte) * 4;
		public PacketType Type { get; set; }

		private const int _dataOffset = _typeOffset + sizeof(ushort);
		public byte[] Data { get; set; }

		private int Crc32Offset { get => _dataOffset + sizeof(byte) * Data.Length; }
		public uint Crc32 { get; set; }

		public byte[] ToBytes()
		{
			byte[] buffer = new byte[Crc32Offset + 4];

			buffer[_appIdOffset + 0] = (byte)((AppId & 0xFF00) >> 8);
			buffer[_appIdOffset + 1] = (byte)((AppId & 0x00FF) >> 0);

			buffer[_seqNumOffset + 0] = (byte)((SequenceNumber & 0xFF00) >> 8);
			buffer[_seqNumOffset + 1] = (byte)((SequenceNumber & 0x00FF) >> 0);

			buffer[_ackSeqNumOffset + 0] = (byte)((AckSequenceNumber & 0xFF00) >> 8);
			buffer[_ackSeqNumOffset + 1] = (byte)((AckSequenceNumber & 0x00FF) >> 0);

			Array.Copy(AckBitfield.ToBytes(), 0, buffer, _ackBitfieldOffset, 4);

			buffer[_typeOffset + 0] = (byte)(((ushort)Type & 0xFF00) >> 8);
			buffer[_typeOffset + 1] = (byte)(((ushort)Type & 0x00FF) >> 0);

			Array.Copy(Data, 0, buffer, _dataOffset, Data.Length);

			buffer[Crc32Offset + 0] = (byte)((Crc32 & 0xFF000000) >> 24);
			buffer[Crc32Offset + 1] = (byte)((Crc32 & 0x00FF0000) >> 16);
			buffer[Crc32Offset + 2] = (byte)((Crc32 & 0x0000FF00) >> 8);
			buffer[Crc32Offset + 3] = (byte)((Crc32 & 0x000000FF) >> 0);

			return buffer;
		}

		public void FromBytes(byte[] buffer)
		{
			FromBytes(buffer, 0, buffer.Length);
		}

		public void FromBytes(byte[] buffer, int offset, int length)
		{
			AppId = (ushort)(
				(buffer[_appIdOffset + offset + 0] << 8) |
				(buffer[_appIdOffset + offset + 1] << 0)
				);

			SequenceNumber = (ushort)(
				(buffer[_seqNumOffset + offset + 0] << 8) |
				(buffer[_seqNumOffset + offset + 1] << 0)
				);

			AckSequenceNumber = (ushort)(
				(buffer[_ackSeqNumOffset + offset + 0] << 8) |
				(buffer[_ackSeqNumOffset + offset + 1] << 0)
				);

			AckBitfield = Injector.CreateInstance<IBitfield>();
			AckBitfield.FromBytes(buffer, _ackBitfieldOffset + offset, 4);

			Type = (PacketType)(
				(buffer[_typeOffset + offset + 0] << 8) |
				(buffer[_typeOffset + offset + 1] << 0)
				);

			Data = new byte[length - (_dataOffset + sizeof(uint))];
			Array.Copy(buffer, _dataOffset + offset, Data, 0, Data.Length);

			Crc32 = (uint)(
				(buffer[Crc32Offset + offset + 0] << 24) |
				(buffer[Crc32Offset + offset + 1] << 16) |
				(buffer[Crc32Offset + offset + 2] << 8) |
				(buffer[Crc32Offset + offset + 3] << 0)
				);
		}
	}
}
