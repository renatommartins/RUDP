using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Enumerations;
using RUDP.Interfaces;

namespace RUDP
{
	public class Packet
	{
		private byte[] _buffer;

		private const int _appIdOffset = 0;
		public ushort AppId
		{
			get
			{
				return (ushort)((_buffer[_appIdOffset + 0] << 8) | (_buffer[_appIdOffset + 1] << 0));
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				_buffer[_appIdOffset + 0] = (byte)((value & 0xFF00) >> 8);
				_buffer[_appIdOffset + 1] = (byte)((value & 0x00FF) >> 0);
			}
		}

		private const int _seqNumOffset = _appIdOffset + sizeof(ushort);
		public ushort SequenceNumber
		{
			get
			{
				return (ushort)((_buffer[_seqNumOffset + 0] << 8) | (_buffer[_seqNumOffset + 1] << 0));
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				_buffer[_seqNumOffset + 0] = (byte)((value & 0xFF00) >> 8);
				_buffer[_seqNumOffset + 1] = (byte)((value & 0x00FF) >> 0);
			}
		}

		private const int _ackSeqNumOffset = _seqNumOffset + sizeof(ushort);
		public ushort AckSequenceNumber 
		{
			get
			{
				return (ushort)((_buffer[_ackSeqNumOffset + 0] << 8) | (_buffer[_ackSeqNumOffset + 1] << 0));
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				_buffer[_ackSeqNumOffset + 0] = (byte)((value & 0xFF00) >> 8);
				_buffer[_ackSeqNumOffset + 1] = (byte)((value & 0x00FF) >> 0);
			}
		}

		private const int _ackBitfieldOffset = _ackSeqNumOffset + sizeof(ushort);
		public Bitfield AckBitfield
		{
			get
			{
				Bitfield bitfield = new Bitfield(_buffer, _ackBitfieldOffset, 4);
				return bitfield;
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				Array.Copy(value.ToBytes(), 0, _buffer, _ackBitfieldOffset, 4);
			}
		}

		private const int _typeOffset = _ackBitfieldOffset + sizeof(byte) * 4;
		public PacketType Type
		{
			get
			{
				return (PacketType)((_buffer[_typeOffset + 0] << 8) | (_buffer[_typeOffset + 1] << 0));
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				_buffer[_typeOffset + 0] = (byte)(((ushort)value & 0xFF00) >> 8);
				_buffer[_typeOffset + 1] = (byte)(((ushort)value & 0x00FF) >> 0);
			}
		}

		private const int _dataOffset = _typeOffset + sizeof(ushort);
		public byte[] Data
		{
			get
			{
				byte[] dataBuffer = new byte[_buffer.Length - (_dataOffset + 4)];
				Array.Copy(_buffer, _dataOffset, dataBuffer, 0, dataBuffer.Length);
				return dataBuffer;
			}
			set
			{
				byte[] newBuffer = new byte[_dataOffset + value.Length + 4];
				Array.Copy(_buffer, 0, newBuffer, 0, _dataOffset);
				Array.Copy(value, 0, newBuffer, _dataOffset, value.Length);
				_buffer = newBuffer;
			}
		}

		private int Crc32Offset { get =>(_buffer.Length - 4); }
		public uint Crc32
		{
			get
			{
				return (uint)(
					(_buffer[Crc32Offset + 0] << 24) |
					(_buffer[Crc32Offset + 1] << 16) |
					(_buffer[Crc32Offset + 2] << 8) |
					(_buffer[Crc32Offset + 3] << 0)
					);
			}
			set
			{
				if (_buffer == null)
					_buffer = new byte[_dataOffset + 4];
				_buffer[Crc32Offset + 0] = (byte)((value & 0xFF000000) >> 24);
				_buffer[Crc32Offset + 1] = (byte)((value & 0x00FF0000) >> 16);
				_buffer[Crc32Offset + 2] = (byte)((value & 0x0000FF00) >> 8);
				_buffer[Crc32Offset + 3] = (byte)((value & 0x000000FF) >> 0);
			}
		}

		public byte[] ToBytes()
		{
			Crc32 = RUDP.Crc32.ComputeChecksum(_buffer, 0, Crc32Offset);

			byte[] buffer = new byte[_buffer.Length];
			Array.Copy(_buffer, 0, buffer, 0, _buffer.Length);

			return buffer;
		}

		public void FromBytes(byte[] buffer)
		{
			FromBytes(buffer, 0, buffer.Length);
		}

		public void FromBytes(byte[] buffer, int offset, int length)
		{
			_buffer = new byte[length];
			Array.Copy(buffer, 0, _buffer, offset, length);
		}

		public bool SequenceNumberGreaterThan(ushort s1, ushort s2)
		{
			return ((s1 > s2) && (s1 - s2 <= ushort.MaxValue / 2)) ||
				   ((s1 < s2) && (s2 - s1 > ushort.MaxValue / 2));
		}

		public bool Validate(ushort appId, ushort lastSequenceNum)
		{
			if(AppId != appId)
				return false;

			if (!SequenceNumberGreaterThan(SequenceNumber, lastSequenceNum))
				return false;

			if (Type >= PacketType.Invalid)
				return false;

			switch(Type)
			{
				case PacketType.ConnectionAccept:
					if (Data.Length < 4)
						return false;
					break;
				case PacketType.Data:
					if (Data.Length == 0)
						return false;
					break;
				default:
					break;
			}

			uint crcCheck = RUDP.Crc32.ComputeChecksum(_buffer, 0, _buffer.Length - 4);
			if (crcCheck != Crc32)
				return false;

			return true;
		}
	}
}
