using System;
using System.Collections.Generic;
using System.Text;

namespace RUDP.Interfaces
{
	public interface IBitfield
	{
		bool this[int index] { get; set; }
		void ShiftRight(int amount);
		void ShiftLeft(int amount);
		byte[] ToBytes();
		void FromBytes(byte[] buffer);
		void FromBytes(byte[] buffer,int offset, int length);
	}
}
