namespace RUDP.Utils
{
	using RUDP.Enumerations;

	/// <summary>
	/// Delivery result for a <see cref="Packet"/>.
	/// </summary>
	public class PacketResult
	{
		/// <summary>
		/// Gets or sets <see cref="Packet"/>.<see cref="Packet.SequenceNumber"/> referenced.
		/// </summary>
		public ushort SequenceNumber { get; set; }

		/// <summary>
		/// Gets or sets <see cref="PacketStatus"/> of the <see cref="Packet"/> referenced.
		/// </summary>
		public PacketStatus Status { get; set; }
	}
}
