using System;
using RUDP.Enumerations;
using RUDP.Utils;
using Xunit;

namespace RUDP.Test;

public class PacketTest
{
	[Fact]
	public void Serialization()
	{
		var expectedBytes = new byte[]
		{
			0x55, 0xAA, //AppId
			0xAA, 0x55, //SequenceNumber
			0x55, 0xAA, //AckSequenceNumber
			0xAA, 0x55, 0xAA, 0x55, //AckBitfield
			0x00, 0x05, //Type
			0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, //Data
			0x64, 0xBD, 0xD2, 0x48 //Crc32
		};
		
		var packet = new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[]{ 0xAA, 0x55, 0xAA, 0x55}, 0 , 4),
			Type = PacketType.Data,
			Data = new ArraySegment<byte>(new byte[]{0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA,}),
		};

		var bytes = packet.ToBytes();

		Assert.Equal(expectedBytes, bytes);
	}

	[Fact]
	public void Deserialization()
	{
		var expectedPacket = new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[]{ 0xAA, 0x55, 0xAA, 0x55}, 0 , 4),
			Type = PacketType.Data,
			Data = new ArraySegment<byte>(new byte[]{0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA,}),
		};
		
		var bytes = new byte[]
		{
			0x55, 0xAA, //AppId
			0xAA, 0x55, //SequenceNumber
			0x55, 0xAA, //AckSequenceNumber
			0xAA, 0x55, 0xAA, 0x55, //AckBitfield
			0x00, 0x05, //Type
			0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, //Data
			0x64, 0xBD, 0xD2, 0x48 //Crc32
		};

		var packet = new Packet(bytes);
		
		Assert.Equal(expectedPacket.AppId, packet.AppId);
		Assert.Equal(expectedPacket.SequenceNumber, packet.SequenceNumber);
		Assert.Equal(expectedPacket.AckSequenceNumber, packet.AckSequenceNumber);
		for(var i = 0; i < expectedPacket.AckBitfield.GetSize(); i++)
			Assert.Equal(expectedPacket.AckBitfield[i], packet.AckBitfield[i]);
		Assert.Equal(expectedPacket.Type, packet.Type);
		Assert.Equal(expectedPacket.Data, packet.Data);
		Assert.Equal(expectedPacket.Crc32, packet.Crc32);
	}
}