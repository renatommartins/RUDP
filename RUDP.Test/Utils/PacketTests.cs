namespace RUDP.Test.Utils;

using System;
using System.Reflection;
using Bogus;
using RUDP.Enumerations;
using RUDP.Utils;
using Xunit;

/// <summary>
/// <see cref="Packet"/> unit tests.
/// </summary>
public class PacketTests
{
	[Fact]
	public void ToBytes_ShouldReturnExpectedByteArray_WhenPacketIsNotNull()
	{
		var expectedBytes = GetSamplePacketBytes();

		var packet = new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[] { 0xAA, 0x55, 0xAA, 0x55 }, 0, 4),
			Type = PacketType.Data,
			Data = new ArraySegment<byte>(new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA }),
		};

		var bytes = Packet.ToBytes(packet);

		Assert.Equal(expectedBytes, bytes);
	}

	[Fact]
	public void ToBytes_ShouldReturnByteArrayWithCorrectCrc32_WhenPacketIsNotNull()
	{
		uint expectedCrc32 = 0x64BDD248u;

		var bytes = Packet.ToBytes(new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[] { 0xAA, 0x55, 0xAA, 0x55 }, 0, 4),
			Type = PacketType.Data,
			Data = new ArraySegment<byte>(new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA }),
		});
		var crc32 = (uint)(
			(bytes[bytes.Length - 4 + 0] << 24) |
			(bytes[bytes.Length - 4 + 1] << 16) |
			(bytes[bytes.Length - 4 + 2] << 8) |
			(bytes[bytes.Length - 4 + 3] << 0));

		Assert.Equal(expectedCrc32, crc32);
	}

	[Fact]
	public void FromBytes_ShouldReturnDeserializedPacket_WhenBufferReceivedIsValid()
	{
		var expectedPacket = new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[] { 0xAA, 0x55, 0xAA, 0x55 }, 0, 4),
			Type = PacketType.Data,
			Data = new ArraySegment<byte>(new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA }),
		};

		var packet = Packet.FromBytes(GetSamplePacketBytes());

		Assert.Equal(expectedPacket.AppId, packet.AppId);
		Assert.Equal(expectedPacket.SequenceNumber, packet.SequenceNumber);
		Assert.Equal(expectedPacket.AckSequenceNumber, packet.AckSequenceNumber);
		for (var i = 0; i < expectedPacket.AckBitfield.GetSize(); i++)
		{
			Assert.Equal(expectedPacket.AckBitfield[i], packet.AckBitfield[i]);
		}

		Assert.Equal(expectedPacket.Type, packet.Type);
		Assert.Equal(expectedPacket.Data, packet.Data);
		Assert.Equal(expectedPacket.Crc32, packet.Crc32);
	}

	[Fact]
	public void FromBytes_ShouldReturnNull_WhenBufferReceivedIsInvalid()
	{
		var faker = new Faker();
		var buffer = faker.Random.Bytes(40);

		var packet = Packet.FromBytes(buffer);

		Assert.Null(packet);
	}

	[Fact]
	public void AppId_ShouldPopulateCacheOnlyAfterFirstCall_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var appId = (ushort)(
			bytes[0] << 8 |
			bytes[1] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_appId", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(appId, packet.AppId);
		Assert.NotNull(
			packetClassType
				.GetField("_appId", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
	}

	[Fact]
	public void AppId_ShouldPopulateCacheOnlyAfterFirstCall_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var appId = (ushort)(
			bytes[0] << 8 |
			bytes[1] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_appId", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		packet.AppId = appId;
		Assert.NotNull(
			packetClassType
				.GetField("_appId", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(
			appId,
			((ushort?)packetClassType.GetField("_appId", BindingFlags.Instance | BindingFlags.NonPublic) !.GetValue(packet)) !.Value);
	}

	[Fact]
	public void AppId_ShouldOnlyApplyCachedValue_WhenInternalBufferIsAccessed()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var expectedAppId = (ushort)(bytes[0] << 8 | bytes[1] << 0);
		var writeExpectedAppId = (ushort)0xAA55;
		var packet = Packet.FromBytes(bytes);

		var packetBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		packet.AppId = writeExpectedAppId;

		Assert.Equal(
			expectedAppId,
			(ushort)(packetBuffer[0] << 8 | packetBuffer[1] << 0));

		_ = Packet.ToBytes(packet);

		Assert.Equal(
			writeExpectedAppId,
			(ushort)(packetBuffer[0] << 8 | packetBuffer[1] << 0));
	}

	[Fact]
	public void SequenceNumber_ShouldPopulateCacheOnlyAfterFirstCall_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var sequenceNumber = (ushort)(bytes[2] << 8 | bytes[3] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_sequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(sequenceNumber, packet.SequenceNumber);
		Assert.NotNull(
			packetClassType
				.GetField("_sequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
	}

	[Fact]
	public void SequenceNumber_ShouldPopulateCacheOnlyAfterFirstCall_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var sequenceNumber = (ushort)(bytes[2] << 8 | bytes[3] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_sequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		packet.SequenceNumber = sequenceNumber;
		Assert.NotNull(
			packetClassType
				.GetField("_sequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(
			sequenceNumber,
			((ushort?)packetClassType.GetField("_sequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !.GetValue(packet)) !.Value);
	}

	[Fact]
	public void SequenceNumber_ShouldOnlyApplyCachedValue_WhenInternalBufferIsAccessed()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var expectedSequenceNumber = (ushort)(bytes[2] << 8 | bytes[3] << 0);
		var writeExpectedSequenceNumber = (ushort)0xAA55;
		var packet = Packet.FromBytes(bytes);

		var packetBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		packet.SequenceNumber = writeExpectedSequenceNumber;

		Assert.Equal(
			expectedSequenceNumber,
			(ushort)(packetBuffer[2] << 8 | packetBuffer[3] << 0));

		_ = Packet.ToBytes(packet);

		Assert.Equal(
			writeExpectedSequenceNumber,
			(ushort)(packetBuffer[2] << 8 | packetBuffer[3] << 0));
	}

	[Fact]
	public void AckSequenceNumber_ShouldPopulateCacheOnlyAfterFirstCall_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var ackSequenceNumber = (ushort)(bytes[4] << 8 | bytes[5] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_ackSequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(ackSequenceNumber, packet.AckSequenceNumber);
		Assert.NotNull(
			packetClassType
				.GetField("_ackSequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
	}

	[Fact]
	public void AckSequenceNumber_ShouldPopulateCacheOnlyAfterFirstCall_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var ackSequenceNumber = (ushort)(bytes[4] << 8 | bytes[5] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_ackSequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		packet.AckSequenceNumber = ackSequenceNumber;
		Assert.NotNull(
			packetClassType
				.GetField("_ackSequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(
			ackSequenceNumber,
			((ushort?)packetClassType.GetField("_ackSequenceNumber", BindingFlags.Instance | BindingFlags.NonPublic) !.GetValue(packet)) !.Value);
	}

	[Fact]
	public void AckSequenceNumber_ShouldOnlyApplyCachedValue_WhenInternalBufferIsAccessed()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var expectedAckSequenceNumber = (ushort)(
			bytes[4] << 8 |
			bytes[5] << 0);
		var writeExpectedAckSequenceNumber = (ushort)0xAA55;
		var packet = Packet.FromBytes(bytes);

		var packetBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		packet.AckSequenceNumber = writeExpectedAckSequenceNumber;

		Assert.Equal(
			expectedAckSequenceNumber,
			(ushort)(packetBuffer[4] << 8 | packetBuffer[5] << 0));

		_ = Packet.ToBytes(packet);

		Assert.Equal(
			writeExpectedAckSequenceNumber,
			(ushort)(packetBuffer[4] << 8 | packetBuffer[5] << 0));
	}

	[Fact]
	public void AckBitfield_ShouldPopulateCacheOnlyAfterFirstCall_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var ackBitfield = new Bitfield(new ArraySegment<byte>(bytes, 6, 4));
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_ackBitfield", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		for (var i = 0; i < ackBitfield.GetBitSize(); i++)
		{
			Assert.Equal(ackBitfield[i], packet.AckBitfield[i]);
		}

		Assert.NotNull(
			packetClassType
				.GetField("_ackBitfield", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
	}

	[Fact]
	public void AckBitfield_ShouldPopulateCacheOnlyAfterFirstCall_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var ackBitfield = new Bitfield(new ArraySegment<byte>(bytes, 6, 4));
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_ackBitfield", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		packet.AckBitfield = ackBitfield;
		Assert.NotNull(
			packetClassType
				.GetField("_ackBitfield", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		var cachedAckBitfield = (Bitfield)packetClassType
			.GetField("_ackBitfield", BindingFlags.Instance | BindingFlags.NonPublic) !
			.GetValue(packet) !;
		for (var i = 0; i < ackBitfield.GetBitSize(); i++)
		{
			Assert.Equal(ackBitfield[i], cachedAckBitfield[i]);
		}
	}

	[Fact]
	public void AckBitfield_ShouldOnlyApplyCachedValue_WhenInternalBufferIsAccessed()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var expectedAckBitfield = new Bitfield(new ArraySegment<byte>(bytes, 6, 4));
		var writeExpectedAckBitfield = new Bitfield(new byte[] { 0x55, 0xAA, 0x55, 0xAA });
		var packet = Packet.FromBytes(bytes);

		var packetBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		packet.AckBitfield = writeExpectedAckBitfield;

		var ackBitfieldBefore = new Bitfield(new ArraySegment<byte>(packetBuffer, 6, 4));
		for (var i = 0; i < expectedAckBitfield.GetBitSize(); i++)
		{
			Assert.Equal(expectedAckBitfield[i], ackBitfieldBefore[i]);
		}

		_ = Packet.ToBytes(packet);

		var ackBitfieldAfter = new Bitfield(new ArraySegment<byte>(packetBuffer, 6, 4));
		for (var i = 0; i < writeExpectedAckBitfield.GetBitSize(); i++)
		{
			Assert.Equal(writeExpectedAckBitfield[i], ackBitfieldAfter[i]);
		}
	}

	[Fact]
	public void Type_ShouldPopulateCacheOnlyAfterFirstCall_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var packetType = (PacketType)(bytes[10] << 8 | bytes[11] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_packetType", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(packetType, packet.Type);
		Assert.NotNull(
			packetClassType
				.GetField("_packetType", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
	}

	[Fact]
	public void Type_ShouldPopulateCacheOnlyAfterFirstCall_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var packetType = (PacketType)(bytes[10] << 8 | bytes[11] << 0);
		var packet = Packet.FromBytes(bytes);

		Assert.Null(
			packetClassType
				.GetField("_packetType", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		packet.Type = packetType;
		Assert.NotNull(
			packetClassType
				.GetField("_packetType", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet));
		Assert.Equal(
			packetType,
			((PacketType?)packetClassType.GetField("_packetType", BindingFlags.Instance | BindingFlags.NonPublic) !.GetValue(packet)) !.Value);
	}

	[Fact]
	public void Type_ShouldOnlyApplyCachedValue_WhenInternalBufferIsAccessed()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var expectedPacketType = (PacketType)(bytes[10] << 8 | bytes[11] << 0);
		var writeExpectedPacketType = (PacketType)0x0005;
		var packet = Packet.FromBytes(bytes);

		var packetBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		packet.Type = writeExpectedPacketType;

		Assert.Equal(
			expectedPacketType,
			(PacketType)(packetBuffer[10] << 8 | packetBuffer[11] << 0));

		_ = Packet.ToBytes(packet);

		Assert.Equal(
			writeExpectedPacketType,
			(PacketType)(packetBuffer[10] << 8 | packetBuffer[11] << 0));
	}

	[Fact]
	public void Data_ShouldAllocateInternalBuffer_WhenInternalBufferIsNullOnAccess()
	{
		var packetClassType = typeof(Packet);
		var packet = new Packet();
		var dataBuffer = new byte[] { 0x55, 0xAA };

		var packetNullDataBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		Assert.Null(packetNullDataBuffer);

		packet.Data = new ArraySegment<byte>(dataBuffer);

		var packetNotNullDataBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		Assert.NotNull(packetNotNullDataBuffer);

		Assert.Equal(dataBuffer, packet.Data);
	}

	[Fact]
	public void Data_ShouldReadFromInternalBuffer_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var packet = Packet.FromBytes(bytes);

		var dataFromProperty = packet.Data.ToArray();

		var packetDataBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		Assert.Equal(dataFromProperty, new ArraySegment<byte>(packetDataBuffer, 12, 8));
	}

	[Fact]
	public void Data_ShouldWriteToInternalBuffer_WhenSetting()
	{
		var packetClassType = typeof(Packet);
		var bytes = GetSamplePacketBytes();
		var packet = Packet.FromBytes(bytes);
		var expectedBeforeWriteData = new ArraySegment<byte>(bytes, 12, 8).ToArray();
		var expectedAfterWriteData = new byte[] { 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55 };

		var packetBeforeWriteDataBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		Assert.Equal(expectedBeforeWriteData, new ArraySegment<byte>(packetBeforeWriteDataBuffer, 12, 8));

		packet.Data = expectedAfterWriteData;

		var packetAfterWriteDataBuffer =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;

		Assert.Equal(expectedAfterWriteData, new ArraySegment<byte>(packetAfterWriteDataBuffer, 12, 8));
	}

	[Fact]
	public void Crc32_ShouldCommitAndReadFromInternalBuffer_WhenGetting()
	{
		var packetClassType = typeof(Packet);
		var expectedCrc = 0xB3C5450C;
		var packet = new Packet()
		{
			AppId = 0x55AA,
			SequenceNumber = 0xAA55,
			AckSequenceNumber = 0x55AA,
			AckBitfield = new Bitfield(new byte[] { 0xAA, 0x55, 0xAA, 0x55 }),
			Type = PacketType.KeepAlive,
		};

		var packetDataBufferBeforeGet =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;
		Assert.Null(packetDataBufferBeforeGet);

		var crc = packet.Crc32;

		var packetDataBufferAfterGet =
			(byte[])packetClassType
				.GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic) !
				.GetValue(packet) !;
		Assert.NotNull(packetDataBufferAfterGet);

		Assert.Equal(expectedCrc, crc);
	}

	private static byte[] GetSamplePacketBytes() =>
		new byte[]
		{
			0x55, 0xAA, // AppId
			0xAA, 0x55, // SequenceNumber
			0x55, 0xAA, // AckSequenceNumber
			0xAA, 0x55, 0xAA, 0x55, // AckBitfield
			0x00, 0x05, // Type
			0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, // Data
			0x64, 0xBD, 0xD2, 0x48, // Crc32
		};
}
