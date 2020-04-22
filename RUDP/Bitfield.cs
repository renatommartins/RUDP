using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Interfaces;

namespace RUDP
{
	public class Bitfield : IBitfield
	{
		private bool[] bitfield;

		public Bitfield()
		{

		}

		/// <summary>
		/// Initializes an empty instance of Bitfield.
		/// </summary>
		/// <param name="byteSize">Size in bytes of the bitfield</param>
		public Bitfield(int byteSize)
		{
			bitField = new bool[byteSize * 8];
		}

		/// <summary>
		/// Gets of sets a bit inside the bitfield.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool this[int index]
		{
			get
			{
				return bitField[index];
			}
			set
			{
				bitField[index] = value;
			}
		}

		/// <summary>
		/// Shifts the bit field right by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount"></param>
		public void ShiftRight(int amount)
		{
			for (int j = 0; j < amount; j++)
			{
				for (int i = bitField.Length - 1; i > 0; i--)
				{
					bitField[i] = bitField[i - 1];
				}
				bitField[0] = false;
			}
		}

		/// <summary>
		/// Shifts the bit field left by <paramref name="amount"/>.
		/// </summary>
		/// <param name="amount"></param>
		public void ShiftLeft(int amount)
		{
			for (int j = 0; j < amount; j++)
			{
				for (int i = 0; i < bitField.Length - 1; i++)
				{
					bitField[i] = bitField[i + 1];
				}
				bitField[bitField.Length - 1] = false;
			}
		}

		/// <summary>
		/// Serializes the Bitfield to raw byte array
		/// </summary>
		/// <returns></returns>
		public byte[] ToBytes()
		{
			byte[] returnArray = new byte[(int)Math.Ceiling((double)bitField.Length / 8)];

			for (int i = 0; i < returnArray.Length; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					byte value = (byte)(bitField[i * 8 + j] ? 1 : 0);
					returnArray[i] |= (byte)(value << (7 - j));
				}
			}

			return returnArray;
		}

		/// <summary>
		/// Deserializes a Bitfield from raw byte array
		/// </summary>
		/// <param name="buffer"></param>
		public void FromBytes(byte[] buffer)
		{
			FromBytes(buffer, 0, buffer.Length);
		}

		public void FromBytes(byte[] buffer, int offset, int length)
		{
			bitField = new bool[length * 8];

			for (int i = 0; i < length; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					if ((buffer[i + offset] & 1 << (7 - j)) > 0)
					{
						bitField[i * 8 + j] = true;
					}
					else
					{
						bitField[i * 8 + j] = false;
					}
				}
			}
		}
	}
}
