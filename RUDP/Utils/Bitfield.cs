namespace RUDP.Utils
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a bitfield.
	/// </summary>
	public class Bitfield
	{
		private bool[] _bitfield;

		/// <summary>
		/// Initializes a new instance of the <see cref="Bitfield"/> class.
		/// </summary>
		/// <param name="byteSize">Size in bytes of the bitfield.</param>
		public Bitfield(int byteSize)
		{
			_bitfield = new bool[byteSize * 8];
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Bitfield"/> class.
		/// </summary>
		/// <param name="buffer">Sequence of bits to initialize the bitfield from.</param>
		/// <param name="offset">Offset from where to start reading from in <paramref name="buffer"/> in bytes.</param>
		/// <param name="length">Length of bytes to read from <paramref name="buffer"/>.</param>
		public Bitfield(IReadOnlyList<byte> buffer, int offset = 0, int? length = 0)
		{
			var lengthToUse = length ?? buffer.Count;
			FromBytesInternal(buffer, offset, lengthToUse);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Bitfield"/> class.
		/// </summary>
		/// <param name="buffer">Sequence of bits to initialize the bitfield from.</param>
		public Bitfield(ArraySegment<byte> buffer)
		{
			FromBytesInternal(buffer.Array, buffer.Offset, buffer.Count);
		}

		/// <summary>
		/// Gets of sets a bit inside the bitfield.
		/// </summary>
		/// <param name="index">Which bit in the bitfield, left to right.</param>
		/// <returns>Value of the bit.</returns>
		public bool this[int index]
		{
			get => _bitfield[index];
			set => _bitfield[index] = value;
		}

		/// <summary>
		/// Serializes the Bitfield to raw byte array.
		/// </summary>
		/// <param name="bitfield">bitfield to be serialized into <see cref="T:byte[]"/>.</param>
		/// <returns>Representation of the bitfield in <see cref="T:byte[]"/>.</returns>
		public static byte[] ToBytes(Bitfield bitfield)
		{
			var returnArray = new byte[(int)Math.Ceiling((double)bitfield._bitfield.Length / 8)];

			for (var i = 0; i < returnArray.Length; i++)
			{
				for (var j = 0; j < 8; j++)
				{
					var value = (byte)(bitfield._bitfield[(i * 8) + j] ? 1 : 0);
					returnArray[i] |= (byte)(value << (7 - j));
				}
			}

			return returnArray;
		}

		/// <summary>
		/// Deserializes <see cref="T:byte[]"/> into <see cref="Bitfield"/>.
		/// </summary>
		/// <param name="buffer"><see cref="T:byte[]"/> to be deserialized.</param>
		/// <param name="offset">Index offset in <paramref name="buffer"/> where to start deserializing from.</param>
		/// <param name="length">Length of bytes to read from <paramref name="buffer"/>.</param>
		/// <returns>Deserialized <see cref="Bitfield"/>.</returns>
		public static Bitfield FromBytes(byte[] buffer, int offset = 0, int? length = null)
		{
			var lengthToUse = length ?? buffer.Length;
			var bitfield = new Bitfield(lengthToUse);
			bitfield.FromBytesInternal(buffer, offset, lengthToUse);

			return bitfield;
		}

		/// <summary>
		/// Set bitfield size in bytes.
		/// </summary>
		/// <param name="byteSize">Size in bytes.</param>
		/// <exception cref="ArgumentOutOfRangeException">When <paramref name="byteSize"/> is negative.</exception>
		public void SetSize(int byteSize)
		{
			if (byteSize < 0)
			{
				throw new ArgumentOutOfRangeException();
			}

			_bitfield = new bool[byteSize * 8];
		}

		/// <summary>
		/// Gets size of the bitfield in bytes.
		/// </summary>
		/// <returns>Size in bytes.</returns>
		public int GetSize()
		{
			var isByteMultiple = _bitfield.Length % 8 == 0;
			return (_bitfield.Length / 8) + (isByteMultiple ? 0 : 1);
		}

		/// <summary>
		/// Gets size of the bitfield in bits.
		/// </summary>
		/// <returns>Size in bits.</returns>
		public int GetBitSize() => _bitfield.Length;

		/// <summary>
		/// Shifts the bit field right by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount">Distance in bits to shift.</param>
		public void ShiftRight(int amount)
		{
			for (var j = 0; j < amount; j++)
			{
				for (var i = _bitfield.Length - 1; i > 0; i--)
				{
					_bitfield[i] = _bitfield[i - 1];
				}

				_bitfield[0] = false;
			}
		}

		/// <summary>
		/// Shifts the bit field left by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount">Distance in bits to shift.</param>
		public void ShiftLeft(int amount)
		{
			for (var j = 0; j < amount; j++)
			{
				for (var i = 0; i < _bitfield.Length - 1; i++)
				{
					_bitfield[i] = _bitfield[i + 1];
				}

				_bitfield[_bitfield.Length - 1] = false;
			}
		}

		/// <summary>
		/// Rotates the bit field right by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount">Distance in bits to rotate.</param>
		public void RotateRight(int amount)
		{
			for (var i = 0; i < amount; i++)
			{
				var carry = _bitfield[0];
				for (var j = 0; j < GetBitSize(); j++)
				{
					var nextIndex = j + 1;
					if (nextIndex >= GetBitSize())
					{
						nextIndex = 0;
					}

					(_bitfield[nextIndex], carry) = (carry, _bitfield[nextIndex]);
				}
			}
		}

		/// <summary>
		/// Rotates the bit field left by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount">Distance in bits to rotate.</param>
		public void RotateLeft(int amount)
		{
			for (var i = 0; i < amount; i++)
			{
				var carry = _bitfield[0];
				for (var j = 0; j < GetBitSize(); j++)
				{
					var nextIndex = j - 1;
					if (nextIndex < 0)
					{
						nextIndex = GetBitSize() - 1;
					}

					(_bitfield[nextIndex], carry) = (carry, _bitfield[nextIndex]);
				}
			}
		}

		private void FromBytesInternal(IReadOnlyList<byte> buffer, int offset = 0, int length = 0)
		{
			_bitfield = new bool[length * 8];

			for (var i = 0; i < length; i++)
			{
				for (var j = 0; j < 8; j++)
				{
					if ((buffer[i + offset] & 1 << (7 - j)) > 0)
					{
						_bitfield[(i * 8) + j] = true;
					}
					else
					{
						_bitfield[(i * 8) + j] = false;
					}
				}
			}
		}
	}
}
