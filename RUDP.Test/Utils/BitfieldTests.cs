using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace RUDP.Test.Utils;

using RUDP.Utils;

/// <summary>
/// <see cref="Bitfield" /> unit tests.
/// </summary>
public class BitfieldTests
{
	[Theory]
	[InlineData(new byte[] { 0x55, 0x55 }, 0)]
	[InlineData(new byte[] { 0x55, 0x55 }, 1)]
	[InlineData(new byte[] { 0x55, 0x55 }, 4)]
	[InlineData(new byte[] { 0x55, 0x55 }, 7)]
	[InlineData(new byte[] { 0x55, 0x55 }, 8)]
	[InlineData(new byte[] { 0x55, 0x55 }, 9)]
	[InlineData(new byte[] { 0x55, 0x55 }, 12)]
	[InlineData(new byte[] { 0x55, 0x55 }, 15)]
	public void Indexer_ShouldReturnNBitFromLeft_WhenGettingIndexN(byte[] source, int bitIndex)
	{
		var bitfield = new Bitfield(source);
		var index = bitIndex / 8;
		var mask = 1 << (7 - (bitIndex % 8));
		var expectedBit = (source[index] & mask) != 0;

		Assert.Equal(expectedBit, bitfield[bitIndex]);
	}

	[Fact]
	public void Indexer_ShouldThrowIndexOutOfRangeException_WhenGettingPositiveIndexAboveBitfieldSize()
	{
		const int bitfieldSize = 4;
		var bitfield = new Bitfield(bitfieldSize);

		Assert.Throws<IndexOutOfRangeException>(() => bitfield[(bitfieldSize * 8) + 1]);
	}

	[Fact]
	public void Indexer_ShouldThrowIndexOutOfRangeException_WhenGettingNegativeIndex()
	{
		var bitfield = new Bitfield(1);

		Assert.Throws<IndexOutOfRangeException>(() => bitfield[-1]);
	}

	[Theory]
	[InlineData(new byte[] { 0x55, 0x55 }, 0)]
	[InlineData(new byte[] { 0x55, 0x55 }, 1)]
	[InlineData(new byte[] { 0x55, 0x55 }, 4)]
	[InlineData(new byte[] { 0x55, 0x55 }, 7)]
	[InlineData(new byte[] { 0x55, 0x55 }, 8)]
	[InlineData(new byte[] { 0x55, 0x55 }, 9)]
	[InlineData(new byte[] { 0x55, 0x55 }, 12)]
	[InlineData(new byte[] { 0x55, 0x55 }, 15)]
	public void Indexer_ShouldSetNBitFromLeft_WhenSettingIndexN(byte[] source, int bitIndex)
	{
		var bitfield = new Bitfield(source);
		var index = bitIndex / 8;
		var mask = 1 << (7 - (bitIndex % 8));
		var expectedBitBeforeWrite = (source[index] & mask) != 0;
		var expectedBitAfterWrite = !expectedBitBeforeWrite;

		var bitBeforeWrite = bitfield[bitIndex];
		bitfield[bitIndex] = expectedBitAfterWrite;
		var bitAfterWrite = bitfield[bitIndex];

		Assert.Equal(expectedBitBeforeWrite, bitBeforeWrite);
		Assert.Equal(expectedBitAfterWrite, bitAfterWrite);
	}

	[Fact]
	public void Indexer_ShouldThrowIndexOutOfRangeException_WhenSettingPositiveIndexAboveBitfieldSize()
	{
		const int bitfieldSize = 4;
		var bitfield = new Bitfield(bitfieldSize);

		Assert.Throws<IndexOutOfRangeException>(() => bitfield[(bitfieldSize * 8) + 1] = false);
	}

	[Fact]
	public void Indexer_ShouldThrowIndexOutOfRangeException_WhenSettingNegativeIndex()
	{
		var bitfield = new Bitfield(1);

		Assert.Throws<IndexOutOfRangeException>(() => bitfield[-1] = false);
	}

	[Theory]
	[InlineData(
		new[] { false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true },
		new byte[] { 0x55, 0x55 })]
	[InlineData(
		new[] { true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false },
		new byte[] { 0xAA, 0xAA })]
	public void ToBytes_ShouldSerializeToEquivalentByteArray(bool[] bitfieldValues, byte[] expected)
	{
		var bitfield = new Bitfield(2);
		for (var i = 0; i < bitfieldValues.Length; i++)
		{
			bitfield[i] = bitfieldValues[i];
		}

		var serializedBitfield = Bitfield.ToBytes(bitfield);

		Assert.Equal(expected, serializedBitfield);
	}

	[Theory]
	[InlineData(
		new byte[] { 0x55, 0x55 },
		new[] { false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true })]
	[InlineData(
		new byte[] { 0xAA, 0xAA },
		new[] { true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false })]
	public void FromBytes_ShouldDeserializeToEquivalentBitfield(byte[] source, bool[] expected)
	{
		var bitfield = Bitfield.FromBytes(source);

		for (var i = 0; i < expected.Length; i++)
		{
			Assert.Equal(expected[i], bitfield[i]);
		}
	}

	[Fact]
	public void SetSize_ShouldResize_WhenSizeIsNotNegative()
	{
		var bitfield = new Bitfield(1);
		var equivalentByteArrayBefore = Bitfield.ToBytes(bitfield);

		bitfield.SetSize(8);
		var equivalentByteArrayAfter = Bitfield.ToBytes(bitfield);

		Assert.Equal(new byte[1], equivalentByteArrayBefore);
		Assert.Equal(new byte[8], equivalentByteArrayAfter);
	}

	[Fact]
	public void SetSize_ShouldThrowArgumentOutOfRange_WhenSizeIsNegative()
	{
		var bitfield = new Bitfield(0);

		Assert.Throws<ArgumentOutOfRangeException>(() => bitfield.SetSize(-1));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(16, 16)]
	public void GetSize_ShouldReturnSizeInEquivalentByteArrayLength(int initSize, int expectedSize)
	{
		var bitfield = new Bitfield(initSize);

		var size = bitfield.GetSize();

		Assert.Equal(expectedSize, size);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 8)]
	[InlineData(4, 32)]
	public void GetBitSize_ShouldReturnSizeInMultiplesOfEight(int initSize, int expectedBitSize)
	{
		var bitfield = new Bitfield(initSize);

		var bitSize = bitfield.GetBitSize();

		Assert.Equal(expectedBitSize, bitSize);
	}

	[Theory]
	[InlineData(new byte[0], new byte[0], 0)]
	[InlineData(new byte[] { 0x55 }, new byte[] { 0x15 }, 2)]
	[InlineData(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, new byte[] { 0x05, 0x55, 0x55, 0x55 }, 5)]
	[InlineData(new byte[] { 0xAA, 0xAA }, new byte[] { 0x02, 0xAA }, 6)]
	public void ShiftRight_ShouldShiftBitfieldFromLeftToRightByNAmountAndDropOverFlow(byte[] source, byte[] expected, int shiftAmount)
	{
		var bitfield = new Bitfield(source);

		bitfield.ShiftRight(shiftAmount);
		var result = Bitfield.ToBytes(bitfield);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(new byte[0], new byte[0], 0)]
	[InlineData(new byte[] { 0x55 }, new byte[] { 0x54 }, 2)]
	[InlineData(new byte[] { 0x55, 0x55, 0x55, 0x55 }, new byte[] { 0xAA, 0xAA, 0xAA, 0xA0 }, 5)]
	[InlineData(new byte[] { 0xAA, 0xAA }, new byte[] { 0xAA, 0x80 }, 6)]
	public void ShiftLeft_ShouldShiftBitfieldFromRightToLeftByNAmountAndDropOverFlow(byte[] source, byte[] expected, int shiftAmount)
	{
		var bitfield = new Bitfield(source);

		bitfield.ShiftLeft(shiftAmount);
		var result = Bitfield.ToBytes(bitfield);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(new byte[0], new byte[0], 0)]
	[InlineData(new byte[] { 0x55 }, new byte[] { 0x55 }, 2)]
	[InlineData(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, new byte[] { 0x55, 0x55, 0x55, 0x55 }, 5)]
	[InlineData(new byte[] { 0xAA, 0xAA }, new byte[] { 0xAA, 0xAA }, 6)]
	public void RotateRight_ShouldRotateFromLeftToRightByNAmountAndNotDiscard(byte[] source, byte[] expected, int rotateAmount)
	{
		var bitfield = new Bitfield(source);

		bitfield.RotateRight(rotateAmount);
		var result = Bitfield.ToBytes(bitfield);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(new byte[0], new byte[0], 0)]
	[InlineData(new byte[] { 0x55 }, new byte[] { 0x55 }, 2)]
	[InlineData(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, new byte[] { 0x55, 0x55, 0x55, 0x55 }, 5)]
	[InlineData(new byte[] { 0xAA, 0xAA }, new byte[] { 0xAA, 0xAA }, 6)]
	public void RotateLeft_ShouldRotateFromRightToLeftByNAmountAndNotDiscard(byte[] source, byte[] expected, int rotateAmount)
	{
		var bitfield = new Bitfield(source);

		bitfield.RotateLeft(rotateAmount);
		var result = Bitfield.ToBytes(bitfield);

		Assert.Equal(expected, result);
	}
}
