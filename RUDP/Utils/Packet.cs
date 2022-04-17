using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RUDP.Test")]

namespace RUDP.Utils
{
	using System;
	using System.Collections.Generic;
	using RUDP.Enumerations;

	/// <summary>
	/// Models data sent between RUDP endpoints.
	/// </summary>
	internal class Packet
	{
		private const int AppIdOffset = 0;
		private const int SeqNumOffset = AppIdOffset + sizeof(ushort);
		private const int AckSeqNumOffset = SeqNumOffset + sizeof(ushort);
		private const int AckBitfieldOffset = AckSeqNumOffset + sizeof(ushort);
		private const int TypeOffset = AckBitfieldOffset + (sizeof(byte) * 4);
		private const int DataOffset = TypeOffset + sizeof(ushort);

		private bool _isDirty;
		private byte[] _buffer;
		private ushort? _appId;
		private ushort? _sequenceNumber;
		private ushort? _ackSequenceNumber;
		private Bitfield _ackBitfield;
		private PacketType? _packetType;
		private uint? _crc32;

		/// <summary>
		/// Gets or sets application identifier to help avoid mismatched and/or unwanted data.
		/// </summary>
		public ushort AppId
		{
			get
			{
				if (_appId is null)
				{
					_appId = (ushort)((_buffer[AppIdOffset + 0] << 8) | (_buffer[AppIdOffset + 1] << 0));
				}

				return _appId.Value;
			}

			set
			{
				if (_appId == value)
				{
					return;
				}

				_appId = value;
				_isDirty = true;
			}
		}

		/// <summary>
		/// Gets or sets identifier in the local sequence for ordering and tracking.
		/// </summary>
		public ushort SequenceNumber
		{
			get
			{
				if (_sequenceNumber is null)
				{
					_sequenceNumber = (ushort)((_buffer[SeqNumOffset + 0] << 8) | (_buffer[SeqNumOffset + 1] << 0));
				}

				return _sequenceNumber.Value;
			}

			set
			{
				if (_sequenceNumber == value)
				{
					return;
				}

				_sequenceNumber = value;
				_isDirty = true;
			}
		}

		/// <summary>
		/// Gets or sets identifier in remote sequence for ordering and tracking.
		/// </summary>
		public ushort AckSequenceNumber
		{
			get
			{
				if (_ackSequenceNumber is null)
				{
					_ackSequenceNumber = (ushort)((_buffer[AckSeqNumOffset + 0] << 8) | (_buffer[AckSeqNumOffset + 1] << 0));
				}

				return _ackSequenceNumber.Value;
			}

			set
			{
				if (_ackSequenceNumber == value)
				{
					return;
				}

				_ackSequenceNumber = value;
				_isDirty = true;
			}
		}

		/// <summary>
		/// Gets or sets acknowledge bitfield for tracking last 32 packets before AckSequenceNumber at remote.
		/// </summary>
		public Bitfield AckBitfield
		{
			get
			{
				if (_ackBitfield is null)
				{
					_ackBitfield = new Bitfield(_buffer, AckBitfieldOffset, 4);
				}

				return _ackBitfield;
			}

			set
			{
				if (_ackBitfield == value)
				{
					return;
				}

				_ackBitfield = value;
				_isDirty = true;
			}
		}

		/// <summary>
		/// Gets or sets type for determining the data purpose.
		/// </summary>
		public PacketType Type
		{
			get
			{
				if (_packetType is null)
				{
					_packetType = (PacketType)((_buffer[TypeOffset + 0] << 8) | (_buffer[TypeOffset + 1] << 0));
				}

				return _packetType.Value;
			}

			set
			{
				if (_packetType == value)
				{
					return;
				}

				_packetType = value;
				_isDirty = true;
			}
		}

		/// <summary>
		/// Gets or sets data being transported.
		/// </summary>
		public ArraySegment<byte> Data
		{
			get => new ArraySegment<byte>(_buffer, DataOffset, _buffer.Length - (DataOffset + sizeof(uint)));
			set
			{
				if (!(value.Array is null) && value.Count > 0)
				{
					var newBuffer = new byte[DataOffset + value.Count + 4];
					if (!(_buffer is null))
					{
						Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
					}

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

		/// <summary>
		/// Gets CRC32 for validating integrity.
		/// </summary>
		public uint Crc32
		{
			get
			{
				if (_crc32 is null || _isDirty)
				{
					CommitToBuffer();
					_crc32 = (uint)(
						(_buffer[Crc32Offset + 0] << 24) |
						(_buffer[Crc32Offset + 1] << 16) |
						(_buffer[Crc32Offset + 2] << 8) |
						(_buffer[Crc32Offset + 3] << 0));
				}

				return _crc32.Value;
			}
		}

		private int Crc32Offset => _buffer.Length - 4;

		/// <summary>
		/// Serializes <paramref name="packet"/> to <see cref="T:byte[]"/>.
		/// </summary>
		/// <param name="packet"><see cref="Packet"/> to be serialized.</param>
		/// <returns><see cref="T:byte[]"/> containing serialized <see cref="Packet"/>.</returns>
		public static byte[] ToBytes(Packet packet)
		{
			packet.CommitToBuffer();

			byte[] buffer = new byte[packet._buffer.Length];
			Array.Copy(packet._buffer, 0, buffer, 0, packet._buffer.Length);

			return buffer;
		}

		/// <summary>
		/// Deserializes <paramref name="buffer"/> to a <see cref="Packet"/>.
		/// </summary>
		/// <param name="buffer"><see cref="T:byte[]"/> containing the data to be deserialized into a <see cref="Packet"/>.</param>
		/// <param name="offset"><paramref name="buffer"/> index offset where to start reading the data from.</param>
		/// <param name="length">amount of indices to read from <paramref name="buffer"/>.</param>
		/// <returns><see cref="Packet"/> deserialized from <paramref name="buffer"/>.</returns>
		public static Packet FromBytes(byte[] buffer, int offset = 0, int? length = null)
		{
			var trueLength = length ?? buffer.Length;

			var calculatedCrc = RUDP.Utils.Crc32.ComputeChecksum(buffer, offset, trueLength - 4);
			var receivedCrc = (uint)(
				(buffer[trueLength - 4 + 0] << 24) |
				(buffer[trueLength - 4 + 1] << 16) |
				(buffer[trueLength - 4 + 2] << 8) |
				(buffer[trueLength - 4 + 3] << 0));
			if (calculatedCrc != receivedCrc)
			{
				return null;
			}

			var packet = new Packet
			{
				_buffer = new byte[length ?? buffer.Length],
			};
			Array.Copy(buffer, 0, packet._buffer, offset, trueLength);

			return packet;
		}

		/// <summary>
		/// Compares <paramref name="s1"/> to <paramref name="s2"/> in a circular sequence.
		/// </summary>
		/// <param name="s1">number being compared.</param>
		/// <param name="s2">reference number.</param>
		/// <returns>whether <paramref name="s1"/> is greater than <paramref name="s2"/>.</returns>
		public static bool SequenceNumberGreaterThan(ushort s1, ushort s2)
		{
			return
				((s1 > s2) && (s1 - s2 <= ushort.MaxValue / 2)) ||
				((s1 < s2) && (s2 - s1 > ushort.MaxValue / 2));
		}

		/// <summary>
		/// Gets list of sequence numbers acknowledged by this <see cref="Packet"/>.
		/// </summary>
		/// <returns>List of acknowledged sequence numbers.</returns>
		public List<ushort> GetAcknowledgedPackets()
		{
			var returnList = new List<ushort>
			{
				AckSequenceNumber,
			};
			for (var i = 0; i < AckBitfield.GetBitSize(); i++)
			{
				if (AckBitfield[i])
				{
					returnList.Add((ushort)(AckSequenceNumber - i - 1));
				}
			}

			return returnList;
		}

		private void CommitToBuffer()
		{
			if (!_isDirty)
			{
				return;
			}

			if (_buffer is null)
			{
				_buffer = new byte[DataOffset + 4];
			}

			// App ID
			_buffer[AppIdOffset + 0] = (byte)((_appId ?? 0 & 0xFF00) >> 8);
			_buffer[AppIdOffset + 1] = (byte)((_appId ?? 0 & 0x00FF) >> 0);

			// Sequence Number
			_buffer[SeqNumOffset + 0] = (byte)((_sequenceNumber ?? 0 & 0xFF00) >> 8);
			_buffer[SeqNumOffset + 1] = (byte)((_sequenceNumber ?? 0 & 0x00FF) >> 0);

			// Ack Sequence Number
			_buffer[AckSeqNumOffset + 0] = (byte)((_ackSequenceNumber ?? 0 & 0xFF00) >> 8);
			_buffer[AckSeqNumOffset + 1] = (byte)((_ackSequenceNumber ?? 0 & 0x00FF) >> 0);

			// Ack Bitfield
			_ackBitfield = _ackBitfield ?? new Bitfield(4);
			Array.Copy(Bitfield.ToBytes(_ackBitfield), 0, _buffer, AckBitfieldOffset, 4);

			// PacketType
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
}
