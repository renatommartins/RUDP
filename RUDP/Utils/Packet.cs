using System;

using RUDP.Enumerations;

namespace RUDP.Utils
{
	public class Packet
	{
		private const int AppIdOffset = 0;
		private const int SeqNumOffset = AppIdOffset + sizeof(ushort);
		private const int AckSeqNumOffset = SeqNumOffset + sizeof(ushort);
		private const int AckBitfieldOffset = AckSeqNumOffset + sizeof(ushort);
		private const int TypeOffset = AckBitfieldOffset + sizeof(byte) * 4;
		private const int DataOffset = TypeOffset + sizeof(ushort);
		private int Crc32Offset => _buffer.Length - 4;

		private bool _isDirty;
		private byte[] _buffer;
		private ushort? _appId;
		private ushort? _sequenceNumber;
		private ushort? _ackSequenceNumber;
		private Bitfield _ackBitfield;
		private PacketType? _packetType;
		private uint? _crc32;
		
		public ushort AppId
		{
			get
			{
				if (_appId is null)
					_appId = (ushort) ((_buffer[AppIdOffset + 0] << 8) | (_buffer[AppIdOffset + 1] << 0));
				return _appId.Value;
			}
			set
			{
				if (_appId == value) return;
				_appId = value;
				_isDirty = true;
			}
		}

		public ushort SequenceNumber
		{
			get
			{
				if (_sequenceNumber is null)
					_sequenceNumber = (ushort) ((_buffer[SeqNumOffset + 0] << 8) | (_buffer[SeqNumOffset + 1] << 0));
				return _sequenceNumber.Value;
			}
			set
			{
				if (_sequenceNumber == value) return;
				_sequenceNumber = value;
				_isDirty = true;
			}
		}

		public ushort AckSequenceNumber 
		{
			get
			{
				if(_ackSequenceNumber is null)
					_ackSequenceNumber = (ushort)((_buffer[AckSeqNumOffset + 0] << 8) | (_buffer[AckSeqNumOffset + 1] << 0));
				return _ackSequenceNumber.Value;
			}
			set
			{
				if (_ackSequenceNumber == value) return;
				_ackSequenceNumber = value;
				_isDirty = true;
			}
		}

		public Bitfield AckBitfield
		{
			get
			{
				if(_ackBitfield is null)
					_ackBitfield = new Bitfield(_buffer, AckBitfieldOffset, 4);
				return _ackBitfield;
			}
			set
			{
				if (_ackBitfield == value) return;
				_ackBitfield = value;
				_isDirty = true;
			}
		}

		public PacketType Type
		{
			get
			{
				if(_packetType is null)
					_packetType = (PacketType)((_buffer[TypeOffset + 0] << 8) | (_buffer[TypeOffset + 1] << 0));
				return _packetType.Value;
			}
			set
			{
				if (_packetType == value) return;
				_packetType = value;
				_isDirty = true;
			}
		}

		public ArraySegment<byte> Data
		{
			get => new ArraySegment<byte>(_buffer, DataOffset, _buffer.Length - (DataOffset + sizeof(uint)));
			set
			{
				if(!(value.Array is null )&& value.Count > 0)
				{
					var newBuffer = new byte[DataOffset + value.Count + 4];
					if(!(_buffer is null))
						Array.Copy(_buffer, 0, newBuffer, 0, DataOffset);
					Array.Copy(value.Array, value.Offset, newBuffer, DataOffset, value.Count);
					_buffer = newBuffer;
				}
				else
				{
					var newBuffer = new byte[DataOffset + 4];
					Array.Copy(_buffer, 0, newBuffer, 0, DataOffset);
					_buffer = newBuffer;
				}

				_isDirty = true;
			}
		}

		public uint Crc32
		{
			get
			{
				if (_crc32 is null || _isDirty)
				{
					CommitToBuffer();
					_crc32 = (uint) (
						(_buffer[Crc32Offset + 0] << 24) |
						(_buffer[Crc32Offset + 1] << 16) |
						(_buffer[Crc32Offset + 2] << 8) |
						(_buffer[Crc32Offset + 3] << 0)
					);
				}
				
				return _crc32.Value;
			}
		}

		public Packet(){}

		public Packet(byte[] buffer)
		{
			FromBytes(buffer, 0, buffer.Length);
		}

		public Packet(byte[] buffer, int offset, int length)
		{
			FromBytes(buffer, offset, length);
		}

		private void CommitToBuffer()
		{
			if (_isDirty)
			{
				//App ID
				_buffer[AppIdOffset + 0] = (byte)((_appId ?? 0 & 0xFF00) >> 8);
				_buffer[AppIdOffset + 1] = (byte)((_appId ?? 0 & 0x00FF) >> 0);
				
				//Sequence Number
				_buffer[SeqNumOffset + 0] = (byte)((_sequenceNumber ?? 0 & 0xFF00) >> 8);
				_buffer[SeqNumOffset + 1] = (byte)((_sequenceNumber ?? 0 & 0x00FF) >> 0);
				
				//Ack Sequence Number
				_buffer[AckSeqNumOffset + 0] = (byte)((_ackSequenceNumber ?? 0 & 0xFF00) >> 8);
				_buffer[AckSeqNumOffset + 1] = (byte)((_ackSequenceNumber ?? 0 & 0x00FF) >> 0);
				
				//Ack Bitfield
				Array.Copy(_ackBitfield.ToBytes(), 0, _buffer, AckBitfieldOffset, 4);
				
				//PacketType
				_buffer[TypeOffset + 0] = (byte)(((ushort?)_packetType ?? 0 & 0xFF00) >> 8);
				_buffer[TypeOffset + 1] = (byte)(((ushort?)_packetType ?? 0 & 0x00FF) >> 0);
				
				var crc32 = RUDP.Utils.Crc32.ComputeChecksum(_buffer, 0, Crc32Offset);
				_buffer[Crc32Offset + 0] = (byte)((crc32 & 0xFF000000) >> 24);
				_buffer[Crc32Offset + 1] = (byte)((crc32 & 0x00FF0000) >> 16);
				_buffer[Crc32Offset + 2] = (byte)((crc32 & 0x0000FF00) >> 8);
				_buffer[Crc32Offset + 3] = (byte)((crc32 & 0x000000FF) >> 0);

				_isDirty = false;
			}
		}

		public byte[] ToBytes()
		{
			CommitToBuffer();

			byte[] buffer = new byte[_buffer.Length];
			Array.Copy(_buffer, 0, buffer, 0, _buffer.Length);

			return buffer;
		}

		public void FromBytes(byte[] buffer, int? offset = null, int? length = null)
		{
			_buffer = new byte[length ?? buffer.Length];
			Array.Copy(buffer, 0, _buffer, offset ?? 0, length ?? buffer.Length);
		}

		public static bool SequenceNumberGreaterThan(ushort s1, ushort s2)
		{
			return
				((s1 > s2) && (s1 - s2 <= ushort.MaxValue / 2)) ||
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
					if (Data.Count < 2)
						return false;
					break;
				case PacketType.Data:
					if (Data.Count == 0)
						return false;
					break;
				default:
					break;
			}

			var crcCheck = RUDP.Utils.Crc32.ComputeChecksum(_buffer, 0, _buffer.Length - 4);
			return crcCheck == Crc32;
		}
	}
}
